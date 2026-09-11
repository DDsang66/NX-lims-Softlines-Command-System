using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
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
    ///   表0 摘要: R0 Test Report Number | 值; R11 空(col0) | Average drying rate (mL/h):(col1-2);
    ///   表1/2/3 结果表: R0 表头(Sample/Start/End/Rate/Average), R1=#1, R2=#2, R3=#3;
    ///   表1 Average 数据列 = #1~#3 纵向合并单格(vMerge restart@R1, continue@R2/R3) → 引擎把两工位
    ///   最终均值一次写进 restart 格; 旧"分割"模板(每行独立格)仍兼容 = 逐行写运行平均
    /// 速率单位 mL/h = 存储 mg/h ÷ 1000（存 mg/h 报告 g/h; 原软件查询列即标 mL/h）。
    /// 无"曲线图"占位段 → 曲线 PNG 追加到文档末尾（用户: aatcc曲线图放在表最后）。
    /// 页脚(footer1, TÜV 签名行): R1 末两格 ____°C / ____%RH = 环境温度/湿度(同克重 PHY_Weight)
    /// </summary>
    public class Aatcc201DocxEngine : IAatcc201DocxEngine, IScopedDependency
    {
        /// <summary>
        /// 报告号字号(半磅): 28 = 14pt。与克重/NF5022 报告同款(三份报告的报告号观感一致)。
        /// 为什么必须显式给: 模板该值格是空的(一个 run 都没有), 取不到样式源 → 只能吃文档默认(10pt),
        /// 反而比左侧 "Test Report Number" 标签的 12pt 还小。14pt 让它压过标签、一眼可见。
        /// </summary>
        private const int ReportNumberFontSizeHalfPoints = 28;

        /// <summary>
        /// 填充 AATCC 201 干燥速率报告(单样品) — 流程地图:
        ///   1. 打开文件, 定位摘要表 + 第一张结果表并做结构校验(结构不符 → 抛异常, 不静默空白);
        ///   2. 表0 摘要: R0 报告号(col1, 加粗 14pt); R11 标签行保持模板原样(不再追加均值);
        ///   3. 表1 结果: R0 Sample 表头格第二行写样品名; 按顺序填 #1/#2 (Start/End/Rate);
        ///      Average 合并格(restart@R1) = 参与工位最终均值, 只写一次; 未参与工位整行留空;
        ///   4. 曲线 PNG → 追加到文档末尾;
        ///   5. 页脚 footer1 末两格: 环境温度(°C)/环境湿度(%RH), 照克重页脚处理;
        ///   6. 保存。OpenXml 操作全部留在本层, 上层只管拼 Aatcc201ReportFillModel。
        /// </summary>
        public void FillReport(string filePath, Aatcc201ReportFillModel model)
        {
            using var doc = WordprocessingDocument.Open(filePath, true);
            var (summary, result) = ValidateTemplate(doc);   // 结构不符 → 抛异常, 不再静默空白

            // 表0 摘要: R0 报告号(col1); R11 平均干燥速率(col0, 标签占 col1-2)
            // 报告号加粗放大: 报告上要一眼可见
            SetCellText(Row(summary, Aatcc201Layout.SummaryRowReportNumber)!, Aatcc201Layout.ValueColumn,
                        model.ReportNumber, bold: true, fontSizeHalfPoints: ReportNumberFontSizeHalfPoints);

            // 表1(第一张结果表)填这个样品; 表2/表3 留空不动 —— 逐张填是合并报告的路径
            FillSampleTable(result, new Aatcc201SampleBlockModel
            {
                SampleName = model.SampleName,
                Stations = model.Stations
            });

            // 曲线图: 模型带 PNG 才嵌入(追加到文档末尾, 模板无占位段)
            if (model.ChartImagePng is { Length: > 0 })
                AppendChartAtEnd(doc, model.ChartImagePng);

            // 页脚: 环境温度/湿度 → footer1 签名行末两格(____°C / ____%RH), 照克重 PHY_Weight
            FillFooter(doc, model.Temperature, model.Humidity);

            doc.MainDocumentPart?.Document?.Save();
        }

        /// <summary>
        /// 填一张 Sample 表 = 一个样品: 表头格第二行写样品名, R1/R2/R3 逐行填该样品的第 1.2.3 次测试,
        /// Average 合并格(restart)一次写最终均值。单样品(FillReport)与合并报告(FillCombinedReport)共用本方法,
        /// 保证"报告里一张 Sample 表怎么长"只有一处实现。
        /// 行填法: 槽位对齐(报告第 i 行 = 模型第 i 项), 未参与/缺槽的项整行留空(允许 1~3 次, 中间缺槽不挤位)。
        /// Average 列新版模板把 #1~#3 合并成单格(vMerge restart@R1, continue@R2/R3): 合并列只认 restart 格内容,
        /// 续格必须空白 —— 所以 3 行只累计, 结束后把最终均值一次写进 restart 格;
        /// 旧模板(分割, 无 vMerge)由 FillStationRow 逐行写运行平均。
        /// </summary>
        private void FillSampleTable(Table result, Aatcc201SampleBlockModel block)
        {
            // Sample 表头格(col0): 原 "Sample" 行保留, 同格第二行写样品名称
            AppendSampleNameUnderHeader(result, block.SampleName);

            int avgMergeRow = FindAverageMergeRow(result);
            int runningCount = 0;
            double runningRate = 0;   // 平均(mL/h = mg/h ÷ 1000), 只统计参与项
            for (int i = 0; i < Aatcc201Layout.SampleRowCount; i++)
                FillStationRow(result, Aatcc201Layout.RowSample1 + i,
                    block.Stations.ElementAtOrDefault(i), ref runningCount, ref runningRate, avgMergeRow >= 0);
            if (avgMergeRow >= 0 && runningCount > 0)
                SetCellText(Row(result, avgMergeRow), Aatcc201Layout.ColumnAverage, (runningRate / runningCount).ToString("F3"));
            // 摘要 R11 "Average drying rate (mL/h):" 行不再填值(均值只出现在结果表合并格)
        }

        /// <summary>
        /// 填合并报告 —— 同一报告号下多个样品合成一份:
        ///   1. 摘要 R0 报告号;
        ///   2. 枚举模板全部 Sample 表(模板自带 3 张), 样品数不超过就用现有表, 超过则克隆空白母本补表;
        ///   3. 每个样品填一张表(顺序 = Samples 顺序, 服务侧按文件生成时间旧→新排);
        ///   4. 所有曲线图按序追加到文档末尾;
        ///   5. 页脚温湿度(服务侧取最新一份文件的值)写入。
        /// 克隆母本在填充前先深拷贝一张空白 Sample 表 —— 模板前几张表马上会被填, 之后再克隆会把数据一起带过去。
        /// </summary>
        public void FillCombinedReport(string filePath, Aatcc201CombinedReportFillModel model)
        {
            using var doc = WordprocessingDocument.Open(filePath, true);
            var (summary, _) = ValidateTemplate(doc);   // 结构不符 → 抛异常, 不产出错位文档

            // 报告号加粗放大(同单样品路径)
            SetCellText(Row(summary, Aatcc201Layout.SummaryRowReportNumber)!, Aatcc201Layout.ValueColumn,
                        model.ReportNumber, bold: true, fontSizeHalfPoints: ReportNumberFontSizeHalfPoints);

            var sampleTables = FindSampleTables(doc);
            if (sampleTables.Count == 0)
                throw new InvalidOperationException("Aatcc201 模板缺少结果表(Sample 表头)");

            // 空白母本: 现在就深拷贝, 保证之后每次克隆拿到的都是没填过的模板表(含 vMerge 合并结构)
            var pristine = (Table)sampleTables[0].CloneNode(true);

            Table? prev = null;
            for (int i = 0; i < model.Samples.Count; i++)
            {
                Table target;
                if (i < sampleTables.Count)
                {
                    target = sampleTables[i];
                }
                else
                {
                    // 第 4 个样品起: 克隆空白表, 插到上一张之后(顺序即 Samples 顺序)
                    target = (Table)pristine.CloneNode(true);
                    (prev ?? sampleTables[^1]).InsertAfterSelf(target);
                }
                FillSampleTable(target, model.Samples[i]);
                prev = target;
            }

            // 曲线图: 全部追加到文档末尾(用户: aatcc 曲线图放在表最后)
            foreach (var png in model.Charts)
                if (png is { Length: > 0 })
                    AppendChartAtEnd(doc, png);

            FillFooter(doc, model.Temperature, model.Humidity);

            doc.MainDocumentPart?.Document?.Save();
        }

        /// <summary>
        /// 解析一份历史报告 docx —— 合并报告的数据源(生成时刻没有落结构化结果, 只能从文件本身读回):
        ///   报告号 = 摘要表 R0 col1;
        ///   样品块 = 每张有数据的 Sample 表(空白表不产出块 → 合并产物再被合并时不会重复带空样品);
        ///   曲线图 = 正文里的图片(正文只有追加在文末的曲线图; 表头/页脚 logo 在别的部件, 不会进来);
        ///   温湿度 = 页脚温湿度表(没填过 → null)。
        /// </summary>
        public Aatcc201ParsedReport ReadReport(string filePath)
        {
            using var doc = WordprocessingDocument.Open(filePath, false);
            var mainPart = doc.MainDocumentPart
                ?? throw new InvalidOperationException($"AATCC 报告缺少主部件: {Path.GetFileName(filePath)}");
            var body = mainPart.Document?.Body
                ?? throw new InvalidOperationException($"AATCC 报告缺少正文: {Path.GetFileName(filePath)}");

            var parsed = new Aatcc201ParsedReport();

            var summary = LocateTable(doc, Aatcc201Layout.SummaryTableMarker);
            parsed.ReportNumber = Row(summary, Aatcc201Layout.SummaryRowReportNumber)?
                .Elements<TableCell>().ElementAtOrDefault(Aatcc201Layout.ValueColumn)?.InnerText.Trim() ?? string.Empty;

            foreach (var table in FindSampleTables(doc))
            {
                var block = ReadSampleBlock(table);
                if (block != null) parsed.Samples.Add(block);
            }

            parsed.Charts.AddRange(ReadBodyChartPngs(mainPart, body));
            (parsed.Temperature, parsed.Humidity) = ReadFooterValues(doc);
            return parsed;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> ReadSampleNames(string filePath)
        {
            try
            {
                using var doc = WordprocessingDocument.Open(filePath, false);
                var names = new List<string>();
                foreach (var table in FindSampleTables(doc))
                {
                    string name = ReadSampleName(table);
                    if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
                }
                return names;
            }
            catch
            {
                // 单个文件读不出来不影响列表其它行(报告坏了不该让历史界面打不开)
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// 结果表 R0 col0 的 "Sample" 表头格内第二行写样品名称——原 "Sample" 行保留, 同格堆两行
        /// 空白样品名不写(不产生空行)。段落样式(对齐/缩进)克隆首段, run 样式克隆格内已有 run。
        /// </summary>
        private static void AppendSampleNameUnderHeader(Table result, string? sampleName)
        {
            if (string.IsNullOrWhiteSpace(sampleName)) return;

            var header = Row(result, Aatcc201Layout.HeaderRow);
            var cell = header?.Elements<TableCell>().ElementAtOrDefault(0);
            if (cell == null) return;

            // 样式源: 段落属性取首段, run 属性取格内带格式的 run(兜底任意 run); 都先克隆再挂, 原节点不动
            var srcPara = cell.Elements<Paragraph>().FirstOrDefault();
            var pPr = srcPara?.ParagraphProperties?.CloneNode(true) as ParagraphProperties;
            var refRun = cell.Descendants<Run>().FirstOrDefault(r => r.RunProperties != null)
                         ?? cell.Descendants<Run>().FirstOrDefault();
            var rp = refRun?.RunProperties?.CloneNode(true) as RunProperties;

            var para = new Paragraph();
            if (pPr != null) para.Append(pPr);
            para.Append(new Run(rp ?? new RunProperties(),
                new Text(sampleName.Trim()) { Space = SpaceProcessingModeValues.Preserve }));
            cell.Append(para);
        }

        /// <summary>
        /// 填一行工位结果: Start(s)/End(s)/Rate(mL/h), 并累计参与数/速率和。
        /// 未参与 → 整行留空。
        /// averageColumnMerged=true(Average 列 vMerge 单格): 不逐行写平均(续格必须空白),
        /// 由 FillReport 结束后把最终均值一次写进 restart 格; false(旧分割布局): 照旧逐行写运行平均。
        /// </summary>
        private void FillStationRow(Table result, int rowIdx, Aatcc201StationRowModel? s,
            ref int runningCount, ref double runningRate, bool averageColumnMerged)
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
            if (!averageColumnMerged)
                SetCellText(r, Aatcc201Layout.ColumnAverage, (runningRate / runningCount).ToString("F3"));
        }

        /// <summary>
        /// 新版模板把结果表 Average 数据列 #1~#3 纵向合并成单格(vMerge restart@首行, continue@后续行):
        /// 从 #1 行起找 Average 格带 vMerge 的首行(即合并锚点/restart 格)。旧模板(每行独立格, 无 vMerge)返回 -1。
        /// 引擎两种布局都支持: 合并 → 最终均值一次写 restart 格, continue 续格保持空白;
        /// 分割 → FillStationRow 逐行写运行平均(旧模板 & 单测内存模板路径)。
        /// </summary>
        private static int FindAverageMergeRow(Table result)
        {
            for (int r = Aatcc201Layout.RowSample1; ; r++)
            {
                var row = Row(result, r);
                if (row == null) return -1;
                var cell = row.Elements<TableCell>().ElementAtOrDefault(Aatcc201Layout.ColumnAverage);
                if (cell?.TableCellProperties?.VerticalMerge != null) return r;
            }
        }

        /// <summary>
        /// 页脚: 把环境温度/湿度填进 footer1 签名行末两格(R1 第 3 格 °C、第 4 格 %RH),
        /// 同克重 PHY_Weight 的页脚处理(模板这三份 TÜV 报告的 footer1 结构一致)。
        /// 按 "%RH" 标记定位 footer, 结构不符立即抛异常; 值空白 → 不填(保留模板横线)。
        /// 参数取字符串而不是模型: 单样品报告(FillReport)与合并报告(FillCombinedReport)共用。
        /// </summary>
        private void FillFooter(WordprocessingDocument doc, string? temperature, string? humidity)
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

            WriteFooterValue(cells[2], temperature, "°C");
            WriteFooterValue(cells[3], humidity, "%RH");

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

        // ============ 解析侧(合并报告的数据源 = 历史 docx 本身) ============

        /// <summary>
        /// 枚举正文里全部 Sample 结果表(模板自带 3 张; 合并报告可能更多)。
        /// 判据 = R0 col0 格文本含 "Sample" —— 填充后该格是 "Sample + 样品名", 仍命中;
        /// 摘要表 R0 col0 是 "Test Report Number", 不会误中。
        /// </summary>
        private static IReadOnlyList<Table> FindSampleTables(WordprocessingDocument doc)
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return Array.Empty<Table>();

            return body.Elements<Table>().Where(IsSampleTable).ToList();
        }

        private static bool IsSampleTable(Table table)
        {
            var header = Row(table, Aatcc201Layout.HeaderRow);
            var first = header?.Elements<TableCell>().ElementAtOrDefault(0);
            return first?.InnerText.Contains("Sample", StringComparison.Ordinal) == true;
        }

        /// <summary>
        /// 读一张 Sample 表: 逐行读 #1~#3 的 Start/End/Rate 还原成结果行模型
        /// (写侧是 int mg/h ÷1000 打 F3, 读回 ×1000 四舍五入即原值, 往返无损),
        /// 一行都读不出数据(= 模板留的空表) → 返回 null, 不算样品。
        /// </summary>
        private static Aatcc201SampleBlockModel? ReadSampleBlock(Table table)
        {
            var stations = new List<Aatcc201StationRowModel>();
            for (int i = 0; i < Aatcc201Layout.SampleRowCount; i++)
            {
                var r = Row(table, Aatcc201Layout.RowSample1 + i);
                var cells = r?.Elements<TableCell>().ToList();
                var rateText = cells?.ElementAtOrDefault(Aatcc201Layout.ColumnRate)?.InnerText;
                if (cells == null ||
                    !double.TryParse(rateText, NumberStyles.Float, CultureInfo.InvariantCulture, out double rateMlPerHour))
                {
                    stations.Add(new Aatcc201StationRowModel { Participated = false });
                    continue;
                }

                int.TryParse(cells.ElementAtOrDefault(Aatcc201Layout.ColumnStartTime)?.InnerText, out int start);
                int.TryParse(cells.ElementAtOrDefault(Aatcc201Layout.ColumnEndTime)?.InnerText, out int end);
                stations.Add(new Aatcc201StationRowModel
                {
                    Participated = true,
                    StartPoint = start,
                    EndPoint = end,
                    // 报告写的是 mL/h(= mg/h ÷ 1000 的 F3 串), 读回换算成存储单位 mg/h
                    RateMgPerHour = (int)Math.Round(rateMlPerHour * 1000.0),
                    RateGPerHour = Math.Round(rateMlPerHour, 3)
                });
            }

            if (!stations.Any(s => s.Participated)) return null;
            return new Aatcc201SampleBlockModel { SampleName = ReadSampleName(table), Stations = stations };
        }

        /// <summary>
        /// 读 Sample 表头格(col0)里的样品名称: 模板首段固定是 "Sample", 样品名写在同一格的第 2 段起
        /// (见 AppendSampleNameUnderHeader); 只剩模板那一段 → 没写样品名, 返回空串。
        /// </summary>
        private static string ReadSampleName(Table table)
        {
            var cell = Row(table, Aatcc201Layout.HeaderRow)?.Elements<TableCell>().ElementAtOrDefault(0);
            if (cell == null) return string.Empty;

            var lines = cell.Elements<Paragraph>().Skip(1)
                .Select(p => p.InnerText.Trim())
                .Where(t => t.Length > 0);
            return string.Join(" ", lines);
        }

        /// <summary>
        /// 读正文里的图片字节(= 引擎追加到文末的曲线图)。遍历正文 blip 关系逐个取 ImagePart 流;
        /// 关系失效/非图片一律跳过(宽容), 一张坏图不影响其它图。
        /// </summary>
        private static List<byte[]> ReadBodyChartPngs(MainDocumentPart mainPart, Body body)
        {
            var pngs = new List<byte[]>();
            foreach (var blip in body.Descendants<A.Blip>())
            {
                string? relId = blip.Embed?.Value;
                if (string.IsNullOrEmpty(relId)) continue;
                try
                {
                    if (mainPart.GetPartById(relId) is not ImagePart image) continue;
                    using var ms = new MemoryStream();
                    using (var stream = image.GetStream()) stream.CopyTo(ms);
                    if (ms.Length > 0) pngs.Add(ms.ToArray());
                }
                catch (ArgumentOutOfRangeException) { /* 关系失效的图: 跳过 */ }
                catch (KeyNotFoundException) { /* 关系缺失的图: 跳过 */ }
            }
            return pngs;
        }

        /// <summary>读页脚温湿度(没填过 → null, 由调用方决定回退到别的文件或留空)。</summary>
        private static (string? Temperature, string? Humidity) ReadFooterValues(WordprocessingDocument doc)
        {
            var footerEl = doc.MainDocumentPart?.FooterParts
                .FirstOrDefault(fp => fp.Footer?.InnerText.Contains("%RH") == true)?.Footer;
            var row = footerEl?.Elements<Table>().FirstOrDefault()?.Elements<TableRow>().ElementAtOrDefault(1);
            var cells = row?.Elements<TableCell>().ToList();
            if (cells == null || cells.Count < 4) return (null, null);

            return (ExtractNumber(cells[2].InnerText), ExtractNumber(cells[3].InnerText));
        }

        /// <summary>取文本里第一个数字(页脚格是 "__23.5__°C" 这种带横线/单位的形态)。</summary>
        private static string? ExtractNumber(string? text)
        {
            var m = Regex.Match(text ?? string.Empty, @"-?\d+(\.\d+)?");
            return m.Success ? m.Value : null;
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
                throw new InvalidOperationException("Aatcc201 模板结果表行数不足(缺 #2(R2) 数据行)");

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

            var para = new Paragraph(new Run(new RunProperties(new NoProof()), drawing));
            // body 级 sectPr 必须是 w:body 最后一个孩子(schema 规定); 直接 Append 会把曲线段排到 sectPr 之后
            // → 文档违例。曲线图本就该在文末 → 插到最后一个 body 级 sectPr 之前(无 sectPr 才 Append 兜底)。
            var lastSectPr = body.Elements<SectionProperties>().LastOrDefault();
            if (lastSectPr != null)
                lastSectPr.InsertBeforeSelf(para);
            else
                body.Append(para);
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

            // 表1 (第一张结果表): R0 表头, R1=#1, R2=#2, R3=#3 —— 3 行都可能被填(允许 1~3 次)
            public const int HeaderRow = 0;
            public const int RowSample1 = 1;   // #1
            public const int RowSample2 = 2;   // #2
            public const int RowSample3 = 3;   // #3
            public const int SampleRowCount = 3;   // 一张 Sample 表最多 3 次测试(R1~R3); 填/读两侧共用
            public const int ColumnStartTime = 1;  // Start time (s)
            public const int ColumnEndTime = 2;    // End time (s)
            public const int ColumnRate = 3;       // Drying rate (mL/h)
            public const int ColumnAverage = 4;    // Average drying rate (mL/h)
            public const int ResultColumnCount = 5;

            // 曲线图显示尺寸(EMU, 1cm=360000): 宽 12cm, 高按图像素 1400:800=7:4 等比 → ≈6.9cm
            // (与 NF5022/Gb21655Layout 同尺寸, 两张图长得一样大; 2026-09-10 用户要求缩小, 原 14cm × 8cm)
            public const long ChartWidthEmu = 12 * 360000L;        // 4,320,000 EMU = 12cm
            public const long ChartHeightEmu = 12 * 360000L * 4 / 7;  // ≈ 2,468,571 EMU ≈ 6.86cm
        }

        private static TableRow? Row(Table? t, int i) => t?.Elements<TableRow>().ElementAtOrDefault(i);

        /// <summary>按坐标写单元格文本(0-based), 保留原样式。空文本清空该格。</summary>
        private void SetCellText(TableRow row, int cellIndex, string text, bool bold = false, int? fontSizeHalfPoints = null)
            => SetCellText(row.Elements<TableCell>().ElementAtOrDefault(cellIndex), text, bold, fontSizeHalfPoints);

        /// <summary>
        /// 写单元格文本, 保留原样式。空文本清空该格。
        /// 流程: ①先抓取原样式(RunProperties) → ②删除多余段落只留首段 → ③删光该段所有 run → ④按新文本重建 run。
        /// 为什么"先抓样式再删内容": 新 run 的样式(字号/字体/加粗)必须从旧 run 复制;
        /// 而旧 run 在步骤③会被删掉, 所以顺序反了就再也取不到样式源, 填进去的字会变成默认格式。
        /// 样式源抓取带兜底: 优先带 rPr 的 run, 其次任意 run(模板值格可能只有裸 run)。
        /// bold/fontSizeHalfPoints = 在该样式源之上再叠加加粗/字号(报告号要一眼可见)。
        /// </summary>
        private void SetCellText(TableCell? cell, string text, bool bold = false, int? fontSizeHalfPoints = null)
        {
            if (cell == null) return;

            // 取样式源: 优先任意带 RunProperties 的 run, 兜底第一个 run; 必须先取——删除多余段落后
            // 后续段落里的 run 会一起被删, 那时再 fallback 就取不到样式了。
            var refRun = cell.Descendants<Run>().FirstOrDefault(r => r.RunProperties != null)
                         ?? cell.Descendants<Run>().FirstOrDefault();
            var rp = refRun?.RunProperties?.CloneNode(true) as RunProperties ?? new RunProperties();

            if (bold) WordEditEngine.MakeBold(rp);
            if (fontSizeHalfPoints != null) WordEditEngine.SetFontSize(rp, fontSizeHalfPoints.Value);

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
