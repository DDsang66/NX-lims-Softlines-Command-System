using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter
{
    /// <summary>
    /// AATCC 201 干燥速率 docx 填充引擎 — 按坐标填格 PHY_AATCC201_DryingRate.docx。
    /// 模板结构（用户提供, 勿改）:
    ///   表0 摘要: R0 Test Report Number | 值; R11 空(col0) | Average drying rate (mL/h):(col1-2) 后追加值
    ///   表1/2/3 结果表: R0 表头(Sample/Start/End/Rate/Average, vMerge), R2=#1, R3=#2, R4=#3
    /// 速率单位 mL/h = 存储 mg/h ÷ 1000（决策7: 存 mg/h 报告 g/h; 原软件查询列即标 mL/h）。
    /// 无"曲线图"占位段 → 曲线 PNG 追加到文档末尾（用户: aatcc曲线图放在表最后）。
    /// 页脚(footer1, TÜV 签名行): R1 末两格 ____°C / ____%RH = 环境温度/湿度(同克重 PHY_Weight)
    /// </summary>
    public class Aatcc201DocxEngine : IAatcc201DocxEngine, IScopedDependency
    {
        /// <summary>
        /// 填充 AATCC 201 干燥速率报告 — 流程地图:
        ///   1. 打开文件, 定位摘要表 + 第一张结果表并做结构校验(结构不符 → 抛异常, 不静默空白);
        ///   2. 表0 摘要: R0 报告号(col1); R11 平均干燥速率(mL/h, 两工位均值, 追加在标签后);
        ///   3. 表1 结果: 按顺序填 #1/#2 (Start/End/Rate/运行平均); 未参与工位整行留空;
        ///   4. 曲线 PNG → 追加到文档末尾;
        ///   5. 页脚 footer1 末两格: 环境温度(°C)/环境湿度(%RH), 照克重页脚处理;
        ///   6. 保存。OpenXml 操作全部留在本层, 上层只管拼 Aatcc201ReportFillModel。
        /// </summary>
        public void FillReport(string filePath, Aatcc201ReportFillModel model)
        {
            using var doc = WordprocessingDocument.Open(filePath, true);
            var (summary, result) = ValidateTemplate(doc);   // 结构不符 → 抛异常, 不再静默空白

            // 表0 摘要: R0 报告号(col1); R11 平均干燥速率(col0, 标签占 col1-2)
            SetCellText(Row(summary, Aatcc201Layout.SummaryRowReportNumber)!, Aatcc201Layout.ValueColumn, model.ReportNumber);

            // 表1(第一张结果表): #1→R2, #2→R3, #3 及表2/表3 留空不动; 未参与工位整行留空
            int runningCount = 0;
            double runningRate = 0;   // 运行平均(mL/h = mg/h ÷ 1000), 只统计参与工位
            FillStationRow(result, Aatcc201Layout.RowSample1, model.Stations.ElementAtOrDefault(0), ref runningCount, ref runningRate);
            FillStationRow(result, Aatcc201Layout.RowSample2, model.Stations.ElementAtOrDefault(1), ref runningCount, ref runningRate);

            double average = runningCount > 0 ? runningRate / runningCount : 0;
            // R11 平均干燥速率: 模板该行是 空(col0) | 标签(col1-2), 值必须跟在标签后(不能填 col0 跑到标签前)。
            // 读标签格原文, 拼成 "标签 值" 复用 SetCellText 写回 —— 不覆盖模板标签, 无需新增追加方法。
            var avgCell = Row(summary, Aatcc201Layout.SummaryRowAverage)!
                .Elements<TableCell>().ElementAtOrDefault(Aatcc201Layout.AverageLabelColumn);
            if (avgCell != null)
                SetCellText(avgCell, avgCell.InnerText.Trim() + " " + average.ToString("F3"));

            // 曲线图: 模型带 PNG 才嵌入(追加到文档末尾, 模板无占位段)
            if (model.ChartImagePng is { Length: > 0 })
                AppendChartAtEnd(doc, model.ChartImagePng);

            // 页脚: 环境温度/湿度 → footer1 签名行末两格(____°C / ____%RH), 照克重 PHY_Weight
            FillFooter(doc, model);

            doc.MainDocumentPart?.Document?.Save();
        }

        /// <summary>
        /// 填一行工位结果: Start(s)/End(s)/Rate(mL/h)/运行平均(mL/h)。
        /// 未参与 → 整行留空(模板 #3 行即自然留空, 本方法不碰它)。
        /// </summary>
        private void FillStationRow(Table result, int rowIdx, Aatcc201StationRowModel? s,
            ref int runningCount, ref double runningRate)
        {
            if (s?.Participated != true) return;

            var r = Row(result, rowIdx);
            if (r == null) return;

            double rateMlPerHour = s.RateMgPerHour / 1000.0;
            SetCellText(r, Aatcc201Layout.ColumnStartTime, s.StartPoint.ToString());
            SetCellText(r, Aatcc201Layout.ColumnEndTime, s.EndPoint.ToString());
            SetCellText(r, Aatcc201Layout.ColumnRate, rateMlPerHour.ToString("F3"));

            runningCount++;
            runningRate += rateMlPerHour;
            SetCellText(r, Aatcc201Layout.ColumnAverage, (runningRate / runningCount).ToString("F3"));
        }

        /// <summary>
        /// 页脚: 把环境温度/湿度填进 footer1 签名行末两格(R1 第 3 格 °C、第 4 格 %RH),
        /// 同克重 PHY_Weight 的页脚处理(模板这三份 TÜV 报告的 footer1 结构一致)。
        /// 按 "%RH" 标记定位 footer, 结构不符立即抛异常; 模型值空白 → 不填(保留模板横线)。
        /// </summary>
        private void FillFooter(WordprocessingDocument doc, Aatcc201ReportFillModel model)
        {
            var footer = doc.MainDocumentPart?.FooterParts
                .FirstOrDefault(fp => fp.Footer?.InnerText.Contains("%RH") == true)
                ?? throw new InvalidOperationException("Aatcc201 模板缺少页脚温湿度表(含 %RH 标记)");
            var footerEl = footer.Footer
                ?? throw new InvalidOperationException("Aatcc201 模板页脚温湿度部件缺失");

            var table = footerEl.Elements<Table>().FirstOrDefault()
                ?? throw new InvalidOperationException("Aatcc201 模板页脚温湿度表缺失表格");

            var row = table.Elements<TableRow>().ElementAtOrDefault(1)
                ?? throw new InvalidOperationException("Aatcc201 模板页脚温湿度表缺 R1(温湿度)行");
            var cells = row.Elements<TableCell>().ToList();
            if (cells.Count < 4)
                throw new InvalidOperationException("Aatcc201 模板页脚温湿度表 R1 格数不足(应含温度/湿度格)");

            WriteFooterValue(cells[2], model.Temperature, "°C");
            WriteFooterValue(cells[3], model.Humidity, "%RH");

            footerEl.Save();
        }

        /// <summary>
        /// 写页脚温湿度格: 数值**居中**于整条横线并加下划线(写在横线上的视觉, 数字带下划线不断线),
        /// 数值两侧仍用 _ 补足到模板原长(补齐 _ 不带下划线, 避免双线), 单位后缀无下划线跟在行尾。
        /// 目标长由模板格子原有的 _ 个数决定: 填前先数(模板横线改长改短会自动适配)。
        /// 值来自页面文本输入(原软件文本输入)——若输入已自带单位(℃/°C、%RH/% 结尾)则原样整串
        /// 居中写在横线上, 不再重复追加; 空白不填(页面没输入 → 模板横线原样保留)。
        /// 拆多 run 的原因: 一个 run 只能有一个下划线属性, 各 run 各自 CloneNode 样式,
        /// 共用同一 rp 对象插到多处会报 "part of a tree"。
        /// </summary>
        private static void WriteFooterValue(TableCell? cell, string? value, string unit)
        {
            if (cell == null || string.IsNullOrWhiteSpace(value)) return;

            value = value.Trim();
            // 输入已带单位 → 整串当下划线值写, 不加单位后缀
            bool hasOwnUnit = unit switch
            {
                "°C" => value.EndsWith('℃') || value.EndsWith("°C", StringComparison.OrdinalIgnoreCase),
                "%RH" => value.EndsWith("%RH", StringComparison.OrdinalIgnoreCase)
                      || value.EndsWith('%'),
                _ => false
            };

            // 原格横线长(_ 个数)必须先取: 下面整格清空后就没参照了; 模板横线长 = 填后横线目标总长
            int blankCols = cell.InnerText.Count(ch => ch == '_');

            // 值居中: 两侧 _ 大致对半分(左 floor 右 ceil), 数字段自带下划线让横线连贯
            int pad = Math.Max(0, blankCols - value.Length);
            int lead = pad / 2, trail = pad - lead;

            // 取样式源: 单元格内任意带 RunProperties 的 run(必须先取——下面删段落会连 run 一起删)
            var refRun = cell.Descendants<Run>().FirstOrDefault(r => r.RunProperties != null)
                         ?? cell.Descendants<Run>().FirstOrDefault();
            var rp = refRun?.RunProperties?.CloneNode(true) as RunProperties;

            // 保留第一个段落, 删除多余段落与全部 run
            var paragraphs = cell.Elements<Paragraph>().ToList();
            for (int i = 1; i < paragraphs.Count; i++) paragraphs[i].Remove();
            var para = paragraphs.FirstOrDefault();
            if (para == null) { para = new Paragraph(); cell.Append(para); }
            foreach (var run in para.Elements<Run>().ToList()) run.Remove();

            // 左侧补齐 _ (无下划线: 模板横线本就是 _ 字形, 再下划线会成双线)
            if (lead > 0)
                para.Append(BuildPadRun(rp, lead));

            // 值 run: 复制原样式 + 下划线(居中位置, 数字带下划线让横线不断)
            var valRun = new Run((rp ?? new RunProperties()).CloneNode(true) as RunProperties ?? new RunProperties());
            valRun.RunProperties!.Underline = new Underline { Val = UnderlineValues.Single };
            valRun.Append(new Text(value));
            para.Append(valRun);

            // 右侧补齐 _
            if (trail > 0)
                para.Append(BuildPadRun(rp, trail));

            // 单位后缀 run: 原样式, 无下划线
            if (!hasOwnUnit)
            {
                var sfxRun = new Run((rp ?? new RunProperties()).CloneNode(true) as RunProperties ?? new RunProperties());
                sfxRun.Append(new Text(unit));
                para.Append(sfxRun);
            }
        }

        /// <summary>造一条 _ 补齐 run(不带下划线, 模板横线本就是 _ 字形)。</summary>
        private static Run BuildPadRun(RunProperties? rp, int count)
        {
            var padRp = (rp ?? new RunProperties()).CloneNode(true) as RunProperties ?? new RunProperties();
            padRp.Underline?.Remove();
            var padRun = new Run(padRp);
            padRun.Append(new Text(new string('_', count)));
            return padRun;
        }

        /// <summary>
        /// 校验 AATCC 模板结构并返回已定位的摘要表 + 第一张结果表。
        /// 锚点逐项断言, 任何一项不符立即抛异常, 不产出错位文档。
        /// </summary>
        private (Table Summary, Table Result) ValidateTemplate(WordprocessingDocument doc)
        {
            var summary = LocateTable(doc, Aatcc201Layout.SummaryTableMarker)
                ?? throw new InvalidOperationException("Aatcc201 模板缺少摘要表(Test Report Number)");
            if (Row(summary, Aatcc201Layout.SummaryRowReportNumber) == null || Row(summary, Aatcc201Layout.SummaryRowAverage) == null)
                throw new InvalidOperationException("Aatcc201 模板摘要表缺 R0(报告号) 或 R11(平均干燥速率) 行");
            if (Row(summary, Aatcc201Layout.SummaryRowReportNumber)!.Elements<TableCell>().Count() < 2)
                throw new InvalidOperationException("Aatcc201 模板摘要表 R0 应有[标签|值]两格");

            var result = LocateTable(doc, Aatcc201Layout.ResultTableMarker)
                ?? throw new InvalidOperationException("Aatcc201 模板缺少结果表(Sample 表头)");
            var header = Row(result, Aatcc201Layout.HeaderRow);
            if (header?.InnerText.Contains("Sample") != true)
                throw new InvalidOperationException("Aatcc201 模板结果表头异常(缺 Sample 列)");
            if (header!.Elements<TableCell>().Count() < Aatcc201Layout.ResultColumnCount)
                throw new InvalidOperationException("Aatcc201 模板结果表头格数不足(应含 Sample/Start/End/Rate/Average)");
            if (Row(result, Aatcc201Layout.RowSample2) == null)
                throw new InvalidOperationException("Aatcc201 模板结果表行数不足(缺到 R3)");

            return (summary, result);
        }

        /// <summary>把曲线 PNG 追加到文档末尾(模板无占位段; 用户: aatcc曲线图放在表最后)。</summary>
        private static void AppendChartAtEnd(WordprocessingDocument doc, byte[] png)
        {
            var mainPart = doc.MainDocumentPart
                ?? throw new InvalidOperationException("Aatcc201 文档主部件缺失");
            var document = mainPart.Document
                ?? throw new InvalidOperationException("Aatcc201 文档主体缺失");
            var body = document.Body
                ?? throw new InvalidOperationException("Aatcc201 文档正文缺失");

            var imagePart = mainPart.AddImagePart(ImagePartType.Png);
            using (var ms = new MemoryStream(png))
                imagePart.FeedData(ms);
            string relId = mainPart.GetIdOfPart(imagePart);

            var drawing = CreateChartDrawing(relId, "Aatcc201Chart",
                Aatcc201Layout.ChartWidthEmu, Aatcc201Layout.ChartHeightEmu);

            body.Append(new Paragraph(new Run(new RunProperties(new NoProof()), drawing)));
        }

        private static Drawing CreateChartDrawing(string relationshipId, string imageName, long widthEmu, long heightEmu)
        {
            return new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                    new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties { Id = 1, Name = imageName },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks { NoChangeAspect = true }
                    ),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties { Id = 1, Name = $"{imageName}.png" },
                                    new PIC.NonVisualPictureDrawingProperties()
                                ),
                                new PIC.BlipFill(
                                    new A.Blip { Embed = relationshipId },
                                    new A.Stretch(new A.FillRectangle())
                                ),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset { X = 0L, Y = 0L },
                                        new A.Extents { Cx = widthEmu, Cy = heightEmu }
                                    ),
                                    new A.PresetGeometry(new A.AdjustValueList())
                                    { Preset = A.ShapeTypeValues.Rectangle }
                                )
                            )
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                    )
                )
                {
                    DistanceFromTop = 0, DistanceFromBottom = 0,
                    DistanceFromLeft = 0, DistanceFromRight = 0
                }
            );
        }

        /// <summary>PHY_AATCC201_DryingRate.docx 模板坐标 — 模板布局一变, 只改这里。即交付给实验室的模板契约。</summary>
        private static class Aatcc201Layout
        {
            // 定位文本 (LocateTable 按 InnerText.Contains 匹配)
            public const string SummaryTableMarker = "Test Report";  // 表0: 摘要表
            public const string ResultTableMarker = "Sample";        // 表1: 第一张结果表(GetTableByContent 取首个含 Sample 的顶层表)

            // 表0 (摘要表)
            public const int SummaryRowReportNumber = 0;   // R0 报告号: [Test Report Number(span2)|值]
            public const int ValueColumn = 1;             // R0 值在第 1 列
            public const int SummaryRowAverage = 11;      // R11 平均干燥速率: [空(col0)|Average drying rate (mL/h):(span2) 后追加值]
            public const int AverageLabelColumn = 1;      // R11 标签格在第 1 列, 值追加在标签文本之后(不能填 col0 → 跑到标签前)

            // 表1 (第一张结果表): R0 表头(vMerge), R2=#1, R3=#2, R4=#3(留空)
            public const int HeaderRow = 0;
            public const int RowSample1 = 2;   // #1
            public const int RowSample2 = 3;   // #2
            public const int ColumnStartTime = 1;  // Start time (s)
            public const int ColumnEndTime = 2;    // End time (s)
            public const int ColumnRate = 3;       // Drying rate (mL/h)
            public const int ColumnAverage = 4;    // Average drying rate (mL/h)
            public const int ResultColumnCount = 5;

            // 曲线图尺寸(EMU, 1cm=360000): 14cm × 8cm
            public const long ChartWidthEmu = 14 * 360000L;
            public const long ChartHeightEmu = 8 * 360000L;
        }

        private static TableRow? Row(Table? t, int i) => t?.Elements<TableRow>().ElementAtOrDefault(i);

        /// <summary>按坐标写单元格文本(0-based), 保留原样式。空文本清空该格。</summary>
        private void SetCellText(TableRow row, int cellIndex, string text)
            => SetCellText(row.Elements<TableCell>().ElementAtOrDefault(cellIndex), text);

        /// <summary>
        /// 写单元格文本, 保留原样式。空文本清空该格。
        /// 流程: ①先抓取原样式(RunProperties) → ②删除多余段落只留首段 → ③删光该段所有 run → ④按新文本重建 run。
        /// 为什么"先抓样式再删内容": 新 run 的样式(字号/字体/加粗)必须从旧 run 复制;
        /// 而旧 run 在步骤③会被删掉, 所以顺序反了就再也取不到样式源, 填进去的字会变成默认格式。
        /// 样式源抓取带兜底: 优先带 rPr 的 run, 其次任意 run(模板值格可能只有裸 run)。
        /// </summary>
        private void SetCellText(TableCell? cell, string text)
        {
            if (cell == null) return;

            // 取样式源: 优先任意带 RunProperties 的 run, 兜底第一个 run; 必须先取——删除多余段落后
            // 后续段落里的 run 会一起被删, 那时再 fallback 就取不到样式了。
            var refRun = cell.Descendants<Run>().FirstOrDefault(r => r.RunProperties != null)
                         ?? cell.Descendants<Run>().FirstOrDefault();
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
        /// 定位表格（支持书签、内容匹配、索引等多种策略）。
        /// 策略优先级: 书签 > 表格内文字 > 表格序号。
        /// </summary>
        private static Table? LocateTable(WordprocessingDocument doc, string identifier)
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

        private static Table? GetTableByIndex(WordprocessingDocument doc, int index)
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return null;

            var tables = body.Elements<Table>().ToList();

            if (index < 0 || index >= tables.Count)
                return null;

            return tables[index];
        }

        private static Table? GetTableByBookmark(WordprocessingDocument doc, string bookmarkName)
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return null;

            var bookmark = body.Descendants<BookmarkStart>()
                .FirstOrDefault(b => b.Name == bookmarkName);

            if (bookmark == null) return null;

            return bookmark.Ancestors<Table>().FirstOrDefault();
        }

        private static Table? GetTableByContent(WordprocessingDocument doc, string searchText)
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return null;

            return body.Elements<Table>()
                .FirstOrDefault(t => t.InnerText.Contains(searchText));
        }
    }
}
