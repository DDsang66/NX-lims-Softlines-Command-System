using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter
{
    /// <summary>
    /// 物理克重 docx 填充引擎 — 按坐标填格 PHY_Weight.docx, 与成分模板(IWordTemplateEngine)完全隔离。
    ///
    /// 设计总览(为什么"按坐标填格"):
    ///   - docx 没有像素坐标, 它是 XML 的树状结构: Document → Table → TableRow → TableCell → Paragraph → Run。
    ///     所以"定位"= 在表格数组里取"第几行的第几个单元格", 而非像画布那样给 x/y。
    ///   - 所有行列坐标集中在嵌套类 PhysicalWeightDocxLayout(本文底部), 是"照着 PHY_Weight.docx
    ///     模板的真实布局人工数出来"的常量。模板增删行列/表头文字时, 只需改那一处。
    ///   - 坐标错位会静默生成错误报告, 所以配套 ValidateTemplate 做结构校验: 模板结构一旦与
    ///     坐标假设不符, 立即抛异常, 宁可失败也不产出"看起来正常实则错位"的文档。
    ///   - 与成分模板(IWordTemplateEngine)完全隔离: 本引擎专管物理克重 PHY_Weight.docx 一种模板。
    /// </summary>
    public class PhysicalWeightDocxEngine : IPhysicalWeightDocxEngine, IScopedDependency
    {
        /// <summary>
        /// 填充物理克重报告 — 流程地图:
        ///   1. 打开文件, 定位两张表并做结构校验(结构不符 → 抛异常, 不静默空白);
        ///   2. 表0 摘要表: 填报告号/测试方法, 按测试类型填汇总网格(测点 + 两种单位均值);
        ///   3. 表1 数据表: 表头写单位, 逐行填数据(超预留行 → 克隆行扩容);
        ///   4. 页脚: 填温湿度(带下划线, 模拟"写在横线上");
        ///   5. 保存。OpenXml 操作全部留在本层, 上层只管拼 PhysicalWeightReportFillModel。
        /// </summary>
        public void FillReport(string filePath, PhysicalWeightReportFillModel model)
        {
            using var doc = WordprocessingDocument.Open(filePath, true);
            var (t0, t1) = ValidateTemplate(doc);   // 结构不符 → 抛异常, 不再静默空白

            SetCellText(Row(t0, PhysicalWeightDocxLayout.SummaryRowReportNumber)!, PhysicalWeightDocxLayout.ValueColumn, model.ReportNumber);
            SetCellText(Row(t0, PhysicalWeightDocxLayout.SummaryRowTestMethod)!,   PhysicalWeightDocxLayout.ValueColumn, model.TestMethod ?? "");

            // 表0 汇总网格: 按测试类型填两列(其余列留空); 超预留行克隆
            var (col1, col2) = PhysicalWeightDocxLayout.SummaryColumnsOf(model.TestType);
            int summaryRow = PhysicalWeightDocxLayout.SummaryDataStartRow;
            foreach (var s in model.SummaryRows)
            {
                var sr = Row(t0, summaryRow);
                if (sr == null)
                {
                    AddRowToTable(t0);
                    sr = Row(t0, summaryRow);
                }
                if (sr == null) break;

                SetCellText(sr, PhysicalWeightDocxLayout.SummarySampleColumn, s.Point);
                SetCellText(sr, col1, s.Value1.ToString("F4"));
                SetCellText(sr, col2, s.Value2.ToString("F4"));
                summaryRow++;
            }

            // 表1 表头: Specimen 单位同行; Average 单位在单元格内换行到下一行
            var headerRow = Row(t1, PhysicalWeightDocxLayout.DataHeaderRow);
            if (headerRow != null)
            {
                SetCellText(headerRow, PhysicalWeightDocxLayout.DataSpecimenCell, $"Specimen ({model.DataUnit})");
                SetCellText(headerRow, PhysicalWeightDocxLayout.DataAverageCell,  $"Average\n({model.DataUnit})");
            }

            int dataRow = PhysicalWeightDocxLayout.DataStartRow;
            foreach (var row in model.Rows)
            {
                var r = Row(t1, dataRow);
                if (r == null)                          // 超过模板预留行 → 克隆最后一数据行
                {
                    AddRowToTable(t1);
                    r = Row(t1, dataRow);
                }
                if (r == null) break;

                SetCellText(r, PhysicalWeightDocxLayout.SampleColumn, row.Point);
                for (int c = 0; c < PhysicalWeightDocxLayout.ValueCount; c++)
                    SetCellText(r, PhysicalWeightDocxLayout.ValueStartColumn + c,
                        c < row.Values.Count ? row.Values[c].ToString("F4") : "");
                SetCellText(r, PhysicalWeightDocxLayout.AverageColumn, row.Average?.ToString("F4") ?? "");
                dataRow++;
            }

            FillFooter(doc, model);

            doc.MainDocumentPart?.Document?.Save();
        }

        /// <summary>
        /// 填写页脚温湿度格子(footer2: R1 第 3 格温度 °C、第 4 格湿度 %RH)。
        /// 按 "%RH" 标记定位 footer, 模板结构不符立即抛异常。
        /// </summary>
        private void FillFooter(WordprocessingDocument doc, PhysicalWeightReportFillModel model)
        {
            var footer = doc.MainDocumentPart?.FooterParts
                .FirstOrDefault(fp => fp.Footer?.InnerText.Contains("%RH") == true)
                ?? throw new InvalidOperationException("PHY_Weight 模板缺少页脚温湿度表(含 %RH 标记)");
            var footerEl = footer.Footer
                ?? throw new InvalidOperationException("PHY_Weight 模板页脚温湿度部件缺失");

            var table = footerEl.Elements<Table>().FirstOrDefault()
                ?? throw new InvalidOperationException("PHY_Weight 模板页脚温湿度表缺失表格");

            var row = table.Elements<TableRow>().ElementAtOrDefault(1)
                ?? throw new InvalidOperationException("PHY_Weight 模板页脚温湿度表缺 R1(温湿度)行");
            var cells = row.Elements<TableCell>().ToList();
            if (cells.Count < 4)
                throw new InvalidOperationException("PHY_Weight 模板页脚温湿度表 R1 格数不足(应含温度/湿度格)");

            if (model.EnvironmentTemperature.HasValue)
                SetFooterValue(cells[2], model.EnvironmentTemperature.Value.ToString("F1"), "°C");
            if (model.EnvironmentHumidity.HasValue)
                SetFooterValue(cells[3], model.EnvironmentHumidity.Value.ToString("F1"), "%RH");

            footerEl.Save();
        }

        /// <summary>
        /// 校验 PHY_Weight.docx 模板结构并返回已定位的两张表。
        ///
        /// 为什么宁可抛异常也不静默: 本引擎的填格依赖 PhysicalWeightDocxLayout 里人工数的坐标。
        /// 模板只要被人改过(增删一行/一列/改表头文字), 坐标就可能整体错位——那种情况下继续填,
        /// 会产出"报告号填到数据行、数值错列"这种表面上能打开、实则全错的 docx, 最难以发现。
        /// 所以这里把 FillReport 会用到的所有锚点(行存在性、列数、标记文字)逐项断言,
        /// 任何一项不符立即抛异常, 让生成失败暴露在调用处, 而不是把错位文档发出去。
        /// </summary>
        private (Table Summary, Table Data) ValidateTemplate(WordprocessingDocument doc)
        {
            var t0 = LocateTable(doc, PhysicalWeightDocxLayout.SummaryTableMarker)
                ?? throw new InvalidOperationException("PHY_Weight 模板缺少摘要表(Test Report Number)");
            if (Row(t0, PhysicalWeightDocxLayout.SummaryRowReportNumber) == null)
                throw new InvalidOperationException("PHY_Weight 模板摘要表行数不足(缺 R0 报告号行)");
            if (Row(t0, PhysicalWeightDocxLayout.SummaryRowTestMethod) == null)
                throw new InvalidOperationException("PHY_Weight 模板摘要表行数不足(缺 R5 测试方法行)");
            if (Row(t0, PhysicalWeightDocxLayout.SummaryHeaderRow) == null)
                throw new InvalidOperationException("PHY_Weight 模板摘要表行数不足(缺 R7 表头行)");
            if (Row(t0, PhysicalWeightDocxLayout.SummaryHeaderRow)!.Elements<TableCell>().Count() < PhysicalWeightDocxLayout.SummaryCellCount)
                throw new InvalidOperationException("PHY_Weight 模板摘要表头格数不足(应含 g/m²、g/m、g/piece 等 8 列)");
            if (Row(t0, PhysicalWeightDocxLayout.SummaryDataStartRow) == null)
                throw new InvalidOperationException("PHY_Weight 模板摘要表没有汇总数据行");
            if (Row(t0, PhysicalWeightDocxLayout.SummaryDataStartRow)!.Elements<TableCell>().Count() < PhysicalWeightDocxLayout.SummaryCellCount)
                throw new InvalidOperationException("PHY_Weight 模板摘要表汇总数据行格数不足");

            var t1 = LocateTable(doc, PhysicalWeightDocxLayout.DataTableMarker)
                ?? throw new InvalidOperationException("PHY_Weight 模板缺少数据表(Specimen)");
            if (Row(t1, PhysicalWeightDocxLayout.HeaderRow1)?.InnerText.Contains("Sample") != true)
                throw new InvalidOperationException("PHY_Weight 模板数据表头异常(缺 Sample 列)");
            if (Row(t1, PhysicalWeightDocxLayout.HeaderRow1)!.Elements<TableCell>().Count() < PhysicalWeightDocxLayout.HeaderCellCount)
                throw new InvalidOperationException("PHY_Weight 模板数据表头格数不足(缺 Specimen/Average)");
            if (Row(t1, PhysicalWeightDocxLayout.HeaderRow2)?.InnerText.Contains("#1") != true)
                throw new InvalidOperationException("PHY_Weight 模板数据表头异常(缺 #1~#5)");
            if (Row(t1, PhysicalWeightDocxLayout.DataStartRow) == null)
                throw new InvalidOperationException("PHY_Weight 模板数据表没有数据行");
            if (Row(t1, PhysicalWeightDocxLayout.DataStartRow)!.Elements<TableCell>().Count() < PhysicalWeightDocxLayout.RowCellCount)
                throw new InvalidOperationException("PHY_Weight 模板数据行格数不足");

            return (t0, t1);
        }

        /// <summary>PHY_Weight.docx 模板坐标 — 模板布局一变, 只改这里</summary>
        private static class PhysicalWeightDocxLayout
        {
            // 定位文本 (LocateTable 按 InnerText.Contains 匹配)
            public const string SummaryTableMarker = "Test Report Number";  // 表0: 摘要表
            public const string DataTableMarker = "Specimen";               // 表1: 数据表

            // 表0 (摘要表) 坐标
            public const int SummaryRowReportNumber = 0;  // R0 报告号
            public const int SummaryRowTestMethod = 5;    // R5 测试方法
            public const int SummaryHeaderRow = 7;        // R7 表头 (8格: Sample|g/m²|oz/yd²|g/m|oz/yd|g/linear meter|g/piece|lb/dozen)
            public const int SummaryDataStartRow = 8;     // R8 汇总网格起始行
            public const int SummarySampleColumn = 0;     // Sample 列
            public const int SummaryCellCount = 8;        // 汇总网格数据行应有格数
            public const int ValueColumn = 1;             // 报告号/方法值所在列

            // 表1 (数据表)
            public const int HeaderRow1 = 1;              // Sample | Specimen | Average
            public const int HeaderRow2 = 2;              // #1 ~ #5
            public const int DataHeaderRow = 1;           // 表头行(写单位)
            public const int DataSpecimenCell = 1;        // Specimen 单元格
            public const int DataAverageCell = 2;         // Average 单元格
            public const int DataStartRow = 3;            // 数据区起始行
            public const int SampleColumn = 0;            // Sample 列 (测点)
            public const int ValueStartColumn = 1;        // 第一个值列
            public const int ValueCount = 5;              // 每行 5 个值
            public const int AverageColumn = 6;           // 平均列
            public const int RowCellCount = 7;            // 数据行应有格数
            public const int HeaderCellCount = 3;         // 表头行应有格数(Sample|Specimen|Average)

            /// <summary>表0 汇总网格双列(0-based): 面积→(1,2), 长度→(3,4), 条重→(6,7)</summary>
            public static (int, int) SummaryColumnsOf(string testType) => testType switch
            {
                "length" => (3, 4),
                "piece" => (6, 7),
                _ => (1, 2)
            };
        }

        private static TableRow? Row(Table? t, int i) => t?.Elements<TableRow>().ElementAtOrDefault(i);

        /// <summary>
        /// 按坐标写单元格文本(0-based), 保留原样式。空文本清空该格。
        /// </summary>
        private void SetCellText(TableRow row, int cellIndex, string text)
            => SetCellText(row.Elements<TableCell>().ElementAtOrDefault(cellIndex), text);

        /// <summary>
        /// 写单元格文本, 保留原样式。空文本清空该格。
        ///
        /// 流程: ①先抓取原样式(RunProperties) → ②删除多余段落只留首段 → ③删光该段所有 run → ④按新文本重建 run。
        /// 为什么要"先抓样式再删内容": 新 run 的样式(字号/字体/加粗)必须从旧 run 复制;
        /// 而旧 run 在步骤③会被删掉, 所以顺序反了就再也取不到样式源, 填进去的字会变成默认格式。
        /// </summary>
        private void SetCellText(TableCell? cell, string text)
        {
            if (cell == null) return;

            // 取样式源: 单元格内任意带 RunProperties 的 run。必须先取——删除多余段落后
            // 后续段落里的 run 会一起被删, 那时再 fallback 就取不到样式了(如页脚湿度格两段落、首段无 run)。
            var refRun = cell.Descendants<Run>().FirstOrDefault(r => r.RunProperties != null);
            var rp = refRun?.RunProperties?.CloneNode(true) as RunProperties;

            // 保留第一个段落, 删除多余段落
            var paragraphs = cell.Elements<Paragraph>().ToList();
            for (int i = 1; i < paragraphs.Count; i++) paragraphs[i].Remove();
            var para = paragraphs.FirstOrDefault();
            if (para == null) { para = new Paragraph(); cell.Append(para); }

            foreach (var run in para.Elements<Run>().ToList()) run.Remove();
            if (string.IsNullOrEmpty(text)) return;

            var newRun = new Run(rp ?? new RunProperties());
            para.Append(newRun);
            TextRunHelper.InsertTextWithLineBreaks(text, newRun);
        }

        /// <summary>
        /// 填页脚温湿度格子: 数值部分加下划线(保留"写在横线上"的视觉), 后缀(°C/%RH)无下划线。
        /// 保留单元格原样式(字号/字体等不变), 空值场景不调用此方法, 模板下划线字符原样保留。
        ///
        /// 为什么拆成两个 run: 一个 run 只能有一个下划线属性, 而我们要"数值有下划线、单位无",
        /// 所以数值和单位各建一个 run, 各带自己的 RunProperties(都克隆自原样式, 单位那个去掉下划线)。
        /// 两个 run 必须各自 CloneNode 样式——若共用同一个 rp 对象插到多处, OpenXml 会报 "part of a tree"。
        /// </summary>
        private void SetFooterValue(TableCell? cell, string value, string suffix)
        {
            if (cell == null) return;

            // 取样式源: 单元格内任意带 RunProperties 的 run。必须先取——删除多余段落后
            // 后续段落里的 run 会一起被删, 那时再 fallback 就取不到样式了(如页脚湿度格两段落、首段无 run)。
            var refRun = cell.Descendants<Run>().FirstOrDefault(r => r.RunProperties != null);
            var rp = refRun?.RunProperties?.CloneNode(true) as RunProperties;

            // 保留第一个段落, 删除多余段落
            var paragraphs = cell.Elements<Paragraph>().ToList();
            for (int i = 1; i < paragraphs.Count; i++) paragraphs[i].Remove();
            var para = paragraphs.FirstOrDefault();
            if (para == null) { para = new Paragraph(); cell.Append(para); }

            foreach (var run in para.Elements<Run>().ToList()) run.Remove();

            // 数值 run: 复制原样式 + 加下划线 (各自克隆, 避免同一 rp 插入多处报 "part of a tree")
            var valRun = new Run((rp ?? new RunProperties()).CloneNode(true) as RunProperties ?? new RunProperties());
            valRun.RunProperties!.Underline = new Underline { Val = UnderlineValues.Single };
            valRun.Append(new Text(value));
            para.Append(valRun);

            // 后缀 run: 原样式, 无下划线
            var sfxRun = new Run((rp ?? new RunProperties()).CloneNode(true) as RunProperties ?? new RunProperties());
            sfxRun.Append(new Text(suffix));
            para.Append(sfxRun);
        }

        /// <summary>
        /// 对特定表格插入新行 — 数据超过模板预留行数时扩容。
        ///
        /// 做法: 克隆"最后一行"(连同样式: 边框/底纹/字体/合并格)追加到表尾, 再清空内容。
        /// 为什么用克隆而不是新建空白行: 新行会丢失模板的边框和字号, 报告里出现"没框的行"很难看;
        /// 克隆保留了完整样式, 只需把文本清掉即可当数据行复用。
        /// 注意: 克隆源是 LastOrDefault(), 若模板最后一行的结构和标准数据行不同(如带合计行),
        /// 克隆出来的行样式会不标准——模板设计时应保证"最后一个预留数据行"落在末行, 或此处改为克隆指定行。
        /// </summary>
        private void AddRowToTable(Table table)
        {
            if (table == null) return;

            var lastRow = table.Elements<TableRow>().LastOrDefault();
            if (lastRow == null) return;

            var newRow = (TableRow)lastRow.CloneNode(true);

            table.Append(newRow);

            foreach (var cell in newRow.Elements<TableCell>())
            {
                ClearCellContent(cell);
            }
        }

        /// <summary>
        /// 清空单元格内容（保留段落结构）
        /// </summary>
        private void ClearCellContent(TableCell cell)
        {
            var paragraphs = cell.Elements<Paragraph>().ToList();

            foreach (var para in paragraphs)
            {
                var runs = para.Elements<Run>().ToList();
                foreach (var run in runs)
                {
                    run.Remove();
                }

                if (!para.HasChildren)
                {
                    para.Append(new Run(new Text("")));
                }
            }
        }

        /// <summary>
        /// 定位表格（支持书签、内容匹配、索引等多种策略）。
        ///
        /// 策略优先级: 书签 > 表格内文字 > 表格序号。
        ///   - 书签: 模板里显式加了书签标记时最稳(文字改动不影响);
        ///   - 内容: 按表格里是否包含某段文字找(如 "Test Report Number")——本引擎默认用这个,
        ///     模板里表头文字变了就定位不到, 会返回 null 再被 ValidateTemplate 抛异常兜住;
        ///   - 索引: 按文档第几张表(0-based), 最脆弱, 模板增删表就错位, 仅作兜底。
        /// </summary>
        private Table? LocateTable(WordprocessingDocument doc, string identifier)
        {
            var table = GetTableByBookmark(doc, identifier);
            if (table != null) return table;

            table = GetTableByContent(doc, identifier);
            if (table != null) return table;

            if (int.TryParse(identifier, out int index))
            {
                table = GetTableByIndex(doc, index);
                if (table != null) return table;
            }

            return null;
        }

        private Table? GetTableByIndex(WordprocessingDocument doc, int index)
        {
            var tables = doc.MainDocumentPart.Document.Body.Elements<Table>().ToList();

            if (index < 0 || index >= tables.Count)
                return null;

            return tables[index];
        }

        private Table? GetTableByBookmark(WordprocessingDocument doc, string bookmarkName)
        {
            var bookmark = doc.MainDocumentPart.Document.Body
                .Descendants<BookmarkStart>()
                .FirstOrDefault(b => b.Name == bookmarkName);

            if (bookmark == null) return null;

            return bookmark.Ancestors<Table>().FirstOrDefault();
        }

        private Table? GetTableByContent(WordprocessingDocument doc, string searchText)
        {
            return doc.MainDocumentPart.Document.Body.Elements<Table>()
                .FirstOrDefault(t => t.InnerText.Contains(searchText));
        }
    }
}
