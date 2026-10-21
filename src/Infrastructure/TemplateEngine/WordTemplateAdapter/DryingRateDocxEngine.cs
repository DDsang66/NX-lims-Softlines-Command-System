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
using NX_lims_Softlines_Command_System.src.Application.Service.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter
{
    /// <summary>
    /// NF5022(GB/T 21655.1) 干燥速率 docx 填充引擎 — 按坐标填格 PHY_GB21655_DryingRate.docx。
    /// 模板结构（用户提供, 勿改; 表号=文档内物理顺序, 定位靠内容标记而非表序号）:
    ///   表0 摘要: R0 Test Report Number | 值; R3 标准引文行(静态); R4 加水量 静态;
    ///             R5 测点 | 值(=样品名称); R7 □洗前□洗后
    ///   表1 干燥速率: 单行"干燥速率：|(g/h)", 值格 = 均值 + 原单位 token
    ///   表2 结果: R0 样品1/2/3; R1 [m0:][值]×3; R4..R24 = 0/3/6..60min 网格,
    ///            每样品 3 列 [时间 | Δmi(span2) | mi]; 6 工位两种排版:
    ///            FillReport = 克隆结果表(旧, 保留); FillReportByPages = 整页克隆+分页符(报告用)
    ///   表3 备注: 静态(最小二乘法说明)
    ///   无"曲线图"占位段 → 每个参与工位(样品)独立一张曲线 PNG 追加到文档末尾
    ///   页脚(footer1, TÜV 签名行): R1 末两格 ____°C / ____%RH = 环境温度/湿度(同克重 PHY_Weight)
    /// "干燥速率："行值格 = 回归斜率(g/h) 均值: FillReport 取全部参与工位; FillReportByPages 按页各算各的
    /// (每页只平均本页那组参与样品 —— 6 样品时 1/2/3 一页、4/5/6 一页)。
    /// 注意"测点："与"干燥速率："两行的**位置会被实验室改动**(两行互换过:
    /// 干燥速率从摘要表 R5 挪进独立单行表、测点进摘要表 R5), 故这两行一律按标签文字定位、
    /// 不绑表序号也不绑表 —— 落在哪张表里都认。
    /// </summary>
    public class DryingRateDocxEngine : IDryingRateDocxEngine, IScopedDependency
    {
        /// <summary>
        /// 报告号字号(半磅): 28 = 14pt。与克重报告同款。
        /// 为什么必须显式给: 模板该值格是空的(一个 run 都没有), 取不到样式源 → 只能吃文档默认(10pt),
        /// 反而比左侧 "Test Report Number" 标签的 12pt 还小。14pt 让它压过标签、一眼可见。
        /// </summary>
        private const int ReportNumberFontSizeHalfPoints = 28;

        /// <summary>
        /// 填充 GB21655 干燥速率报告 — 流程地图:
        ///   1. 打开文件, 定位摘要表/测点行/干燥速率行/结果表并做结构校验(结构不符 → 抛异常, 不静默空白);
        ///   2. 摘要表: 报告号; "干燥速率："行值格 = 均值(参与工位回归斜率, g/h) + 模板原单位 token;
        ///   3. "测点："行值格 = 样品名称(模板无独立"样品名称"格);
        ///   4. 表2 结果: 每 3 样品一组填 m0 + Δmi/mi 时间网格; 超 3 样品 → 克隆整表扩容
        ///      (整组无参与工位 → 不产生空表); 表头样品号=工位号;
        ///   5. 曲线: 每个参与工位(样品)独立一张 PNG(模型 ChartPngs)按序追加到文档末尾(模板无占位段);
        ///   6. 页脚 footer1 末两格: 环境温度(°C)/环境湿度(%RH), 照克重页脚处理;
        ///   7. 保存。OpenXml 操作全部留在本层, 上层只管拼 DryingRateReportFillModel。
        /// </summary>
        public void FillReport(string filePath, DryingRateReportFillModel model)
        {
            using var doc = WordprocessingDocument.Open(filePath, true);
            var (summary, sampleNameCell, rateCell, result) = ValidateTemplate(doc);   // 结构不符 → 抛异常, 不再静默空白

            // 摘要表 报告号; R3 标准引文/R4 加水量/R7 洗前洗后为模板静态内容
            // 报告号加粗放大: 报告上要一眼可见
            SetCellText(Row(summary, Gb21655Layout.SummaryRowReportNumber)!, Gb21655Layout.ValueColumn,
                        model.ReportNumber, bold: true, fontSizeHalfPoints: ReportNumberFontSizeHalfPoints);

            // 样品名称 → "测点："行值格(模板无独立"样品名称"格)
            SetCellText(sampleNameCell, model.SampleName);

            var participated = model.Stations.Where(s => s.Participated).ToList();
            if (participated.Count > 0)
            {
                double avgRateGPerHour = participated.Average(s => s.RateGPerHour);
                // 模板值格原内容 = 单位 token"(g/h)"([干燥速率：, (g/h)])——SetCellText 整格重建会把它冲掉,
                // 故先取回再拼在数值后。
                string unit = rateCell.InnerText.Trim();
                string value = avgRateGPerHour.ToString("F3");
                SetCellText(rateCell, unit.Length > 0 ? $"{value} {unit}" : value, bold: true);
            }

            // 结果表: 每 3 样品一组; 整组无参与工位 → 不产生空表
            Table current = result;
            for (int start = 0; start < model.Stations.Count; start += Gb21655Layout.SamplesPerTable)
            {
                var group = model.Stations.Skip(start).Take(Gb21655Layout.SamplesPerTable).ToList();
                if (!group.Any(s => s.Participated)) continue;

                // 第一组用模板自带表; 之后每组深拷贝整表扩容(克隆逻辑抽到 WordEditEngine.CloneTableAfter)
                if (start > 0)
                    current = WordEditEngine.CloneTableAfter(current);
                FillSampleGroup(current, group, model.SpaceTimeMin);
            }

            // 曲线图: 每个参与工位(样品)独立一张 PNG →
            // 按序追加到文档末尾(模板无占位段), 图内已自带头"样品N 蒸发曲线"
            for (int i = 0; i < model.ChartPngs.Count; i++)
                if (model.ChartPngs[i] is { Length: > 0 })
                    AppendChartAtEnd(doc, model.ChartPngs[i], i);

            // 页脚: 环境温度/湿度 → footer1 签名行末两格(____°C / ____%RH), 照克重 PHY_Weight
            FillFooter(doc, model);

            doc.MainDocumentPart?.Document?.Save();
        }

        /// <summary>
        /// 填充 GB21655 干燥速率报告 —— 整页克隆版(报告新布局)。每个参与组(≤3 工位)独占一整页:
        /// 第 2 组起把模板"整页"块(摘要+测点+结果+Equipment+备注, 见 <see cref="CollectTemplatePage"/>)
        /// 整页深拷贝成新页, 页前加分页符(同一节内分页 → 每页共享模板页脚/页边距)。
        /// 摘要(报告号)/样品名称/Equipment/备注随页重复; "干燥速率"均值则按页各算各的
        /// (每页只对本页那组参与样品取平均 —— 6 样品时 1/2/3 一页、4/5/6 一页)。
        /// 与 FillReport(整表克隆)的分工: FillReport 保留原行为供回归对照; 报告生成走本方法。
        /// 单组(≤3 工位)时与 FillReport 输出一致(不克隆, 原地填模板页)。
        /// </summary>
        public void FillReportByPages(string filePath, DryingRateReportFillModel model)
        {
            using var doc = WordprocessingDocument.Open(filePath, true);
            var body = doc.MainDocumentPart?.Document?.Body
                ?? throw new InvalidOperationException("GB21655 模板正文缺失");
            var (summary, sampleNameCell, rateCell, result) = ValidateTemplate(doc);   // 结构不符 → 抛异常, 不再静默空白

            // 摘要表 报告号; R3 标准引文/R4 加水量/R7 洗前洗后为模板静态内容
            // 报告号加粗放大: 报告上要一眼可见
            SetCellText(Row(summary, Gb21655Layout.SummaryRowReportNumber)!, Gb21655Layout.ValueColumn,
                        model.ReportNumber, bold: true, fontSizeHalfPoints: ReportNumberFontSizeHalfPoints);

            // 样品名称 → "测点："行值格(模板无独立"样品名称"格)
            SetCellText(sampleNameCell, model.SampleName);

            // 参与工位按 3 个一组切页; 整组无参与工位 → 该组不出页。
            //   第 1 组直接用模板自带整页; 之后每组整页克隆。克隆时机在填网格之前:
            //   摘要/样品名已填(克隆页继承同款报告头), 结果网格此刻仍空 → 克隆不会把上一组数据带进新页。
            //   "干燥速率"均值不在这里填: 它按页各算各的(6 样品 1/2/3、4/5/6 分开算), 必须等每页克隆出来
            //   再各写各的; 若先填模板页, 克隆页会把第 1 组的数当成模板原单位 token 继承过去。
            var groups = new List<List<DryingRateStationRowModel>>();
            for (int start = 0; start < model.Stations.Count; start += Gb21655Layout.SamplesPerTable)
            {
                var group = model.Stations.Skip(start).Take(Gb21655Layout.SamplesPerTable).ToList();
                if (group.Any(s => s.Participated)) groups.Add(group);
            }

            var pageBlock = CollectTemplatePage(doc);        // 模板整页元素块(克隆源)
            var rateTable = rateCell.Ancestors<Table>().FirstOrDefault();   // 速率行所在表(模板改版可能搬表, 按祖先取)
            var pageTables = new List<Table> { result };     // 每组一页: [0]=模板自带页
            var pageRateCells = new List<TableCell> { rateCell };
            for (int gi = 1; gi < groups.Count; gi++)
            {
                var clones = WordEditEngine.ClonePageBlock(body, pageBlock);   // 分页符段+整页深拷贝 → 文末 sectPr 前
                Table? pageResult = null, pageRate = null;
                for (int i = 0; i < pageBlock.Count; i++)
                {
                    if (ReferenceEquals(pageBlock[i], result)) pageResult = clones[i] as Table;
                    if (rateTable != null && ReferenceEquals(pageBlock[i], rateTable)) pageRate = clones[i] as Table;
                }
                if (pageResult == null)
                    throw new InvalidOperationException("GB21655 模板页块缺结果表, 无法克隆新页");
                pageTables.Add(pageResult);
                pageRateCells.Add(FindRateValueCell(pageRate)
                    ?? throw new InvalidOperationException("GB21655 克隆页缺干燥速率行, 无法按页填均值"));
            }

            // 每页各填自己的 3 个样品槽(表头样品号=工位号、m0、0..60min 网格; 未参与留空)
            // 与自己的速率均值(只取本页这组参与样品)
            for (int gi = 0; gi < groups.Count; gi++)
            {
                FillPageRate(pageRateCells[gi], groups[gi]);
                FillSampleGroup(pageTables[gi], groups[gi], model.SpaceTimeMin);
            }

            // 曲线图: 每个参与工位(样品)独立一张 PNG →
            // 按序追加到文档末尾(模板无占位段), 图内已自带头"样品N 蒸发曲线"
            for (int i = 0; i < model.ChartPngs.Count; i++)
                if (model.ChartPngs[i] is { Length: > 0 })
                    AppendChartAtEnd(doc, model.ChartPngs[i], i);

            // 页脚: 环境温度/湿度 → footer1 签名行末两格(____°C / ____%RH), 照克重 PHY_Weight
            FillFooter(doc, model);

            doc.MainDocumentPart?.Document?.Save();
        }

        /// <summary>
        /// 解析一份历史 NF5022 报告 —— 合并报告的数据源(生成时刻没有另存结构化结果)。
        /// 按正文分页符切页: 每页 = 一次测试(≤3 样品); 报告号/测点/干燥速率一律按标签在页内找,
        /// 兼容"测点/干燥速率两行互换"前后的两种排版;
        /// 结果表每样品 [时间|Δmi|mi] 三列, 由 Δmi 还原蒸发曲线、由 mi(0) 还原滴水量。
        /// 数值全部读回历史报告印出来的数, 不重算单样品速率(报告本来就没印)。
        /// </summary>
        public Nf5022ParsedReport ReadReport(string filePath)
        {
            using var doc = WordprocessingDocument.Open(filePath, false);
            var mainPart = doc.MainDocumentPart
                ?? throw new InvalidOperationException($"NF5022 报告缺少主部件: {Path.GetFileName(filePath)}");
            var body = mainPart.Document?.Body
                ?? throw new InvalidOperationException($"NF5022 报告缺少正文: {Path.GetFileName(filePath)}");

            var parsed = new Nf5022ParsedReport();

            var summary = LocateTable(doc, Gb21655Layout.SummaryTableMarker);
            parsed.ReportNumber = Row(summary, Gb21655Layout.SummaryRowReportNumber)?
                .Elements<TableCell>().ElementAtOrDefault(Gb21655Layout.ValueColumn)?.InnerText.Trim() ?? string.Empty;

            foreach (var segment in SplitReportPages(body))
            {
                var page = ReadPage(segment);
                if (page != null) parsed.Pages.Add(page);
            }
            if (parsed.Pages.Count == 0)
                throw new InvalidOperationException("报告里没有可读取的结果表");

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
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body == null) return Array.Empty<string>();

                var names = new List<string>();
                foreach (var segment in SplitReportPages(body))
                {
                    string name = FindValueCellIn(segment, Gb21655Layout.MeasurePointLabel)?.InnerText.Trim()
                                  ?? string.Empty;
                    if (name.Length > 0 && !names.Contains(name)) names.Add(name);
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
        /// 填充合并报告 —— 同一报告号下多份历史报告, 每次测试原样成一页:
        ///   1. 第 2 页起整页克隆模板页(同 FillReportByPages);
        ///   2. 每页填自己的样品名称、干燥速率均值(历史报告上印的值, 原样沿用)、结果网格(用本页采样间隔);
        ///   3. 曲线图全部追加到文末; 页脚温湿度只填一次(一份文档共用一个页脚)。
        /// 克隆必须先于填充: 模板页值格的单位 token 与空网格要保持原样被克隆,
        /// 否则第 1 页的数会被当成模板内容带进后续页。
        /// </summary>
        public void FillCombinedReport(string filePath, DryingRateCombinedReportFillModel model)
        {
            using var doc = WordprocessingDocument.Open(filePath, true);
            var body = doc.MainDocumentPart?.Document?.Body
                ?? throw new InvalidOperationException("GB21655 模板正文缺失");
            var (_, _, _, result) = ValidateTemplate(doc);

            if (model.Pages.Count == 0)
                throw new InvalidOperationException("合并报告没有可填充的页");

            var pageBlock = CollectTemplatePage(doc);
            var pageTables = new List<Table> { result };
            var pageBlocks = new List<List<OpenXmlElement>> { pageBlock };

            for (int pi = 1; pi < model.Pages.Count; pi++)
            {
                var clones = WordEditEngine.ClonePageBlock(body, pageBlock);
                Table? pageResult = null;
                for (int i = 0; i < pageBlock.Count; i++)
                {
                    if (ReferenceEquals(pageBlock[i], result)) pageResult = clones[i] as Table;
                }
                if (pageResult == null)
                    throw new InvalidOperationException("GB21655 模板页块缺结果表, 无法克隆新页");
                pageTables.Add(pageResult);
                pageBlocks.Add(clones);
            }

            for (int pi = 0; pi < model.Pages.Count; pi++)
            {
                var page = model.Pages[pi];
                var block = pageBlocks[pi];

                var summary = block.OfType<Table>()
                    .FirstOrDefault(t => t.InnerText.Contains(Gb21655Layout.SummaryTableMarker))
                    ?? throw new InvalidOperationException("GB21655 报告页缺少摘要表(Test Report)");
                SetCellText(Row(summary, Gb21655Layout.SummaryRowReportNumber)!, Gb21655Layout.ValueColumn,
                            model.ReportNumber, bold: true, fontSizeHalfPoints: ReportNumberFontSizeHalfPoints);

                var sampleNameCell = FindValueCellIn(block, Gb21655Layout.MeasurePointLabel)
                    ?? throw new InvalidOperationException("GB21655 报告页缺少样品名称行(测点：)");
                SetCellText(sampleNameCell, page.SampleName);

                var rateCell = FindValueCellIn(block, Gb21655Layout.RateLabel)
                    ?? throw new InvalidOperationException("GB21655 报告页缺少干燥速率行(干燥速率：)");
                FillRateValue(rateCell, page.RateGPerHour ?? 0);

                FillSampleGroup(pageTables[pi], page.Stations, page.SpaceTimeMin);
            }

            for (int i = 0; i < model.ChartPngs.Count; i++)
                if (model.ChartPngs[i] is { Length: > 0 })
                    AppendChartAtEnd(doc, model.ChartPngs[i], i);

            FillFooter(doc, model.Temperature, model.Humidity);

            doc.MainDocumentPart?.Document?.Save();
        }

        /// <summary>
        /// 把正文按分页符切成"页": 每段 = 一次测试(摘要/测点/速率/结果/Equipment/备注)。
        /// 前导分节段(带 w:sectPr 的空段)与文末 body 级 sectPr 不算内容; 分页符段本身跳过(它属于下一页)。
        /// 没有分页符的单页报告 = 一个段; 文末追加的曲线图段没有结果表, 由 ReadPage 返回 null 跳过。
        /// </summary>
        private static List<List<OpenXmlElement>> SplitReportPages(Body body)
        {
            var pages = new List<List<OpenXmlElement>>();
            var current = new List<OpenXmlElement>();

            foreach (var e in body.ChildElements)
            {
                if (e is SectionProperties) break;   // 文末 body 级节属性之后不再有内容
                if (e is Paragraph p)
                {
                    // 前导分节空段不属于页内容(与 CollectTemplatePage 一致)
                    if (p.ParagraphProperties?.SectionProperties is not null) continue;
                    if (p.Descendants<Break>().Any(b => b.Type?.Value == BreakValues.Page))
                    {
                        if (current.Count > 0) { pages.Add(current); current = new List<OpenXmlElement>(); }
                        continue;
                    }
                }
                current.Add(e);
            }
            if (current.Count > 0) pages.Add(current);
            return pages;
        }

        /// <summary>
        /// 读一页历史报告: 结果表按 3 槽还原样品行; 关键值(均值/滴水量/曲线)读不出来 → 抛说明性异常。
        /// 一页里出现两张结果表 = 旧整表克隆布局(一个摘要两张结果表、无分页符), 明确拒绝合并,
        /// 不做"猜哪张属于哪次测试"的拆分。
        /// </summary>
        private DryingRateParsedPageModel? ReadPage(List<OpenXmlElement> segment)
        {
            var resultTables = segment.OfType<Table>()
                .Where(t => t.InnerText.Contains(Gb21655Layout.ResultTableMarker))
                .Where(HasSampleHeader)
                .ToList();
            if (resultTables.Count == 0) return null;
            if (resultTables.Count > 1)
                throw new InvalidOperationException("旧版布局不支持合并(一个摘要两张结果表)");

            var table = resultTables[0];

            var rateCell = FindValueCellIn(segment, Gb21655Layout.RateLabel)
                ?? throw new InvalidOperationException("报告页缺少干燥速率行(干燥速率：)");
            double? rate = ParseFirstNumber(rateCell.InnerText);
            if (rate == null)
                throw new InvalidOperationException("报告页的干燥速率不是数字");

            var page = new DryingRateParsedPageModel
            {
                SampleName = FindValueCellIn(segment, Gb21655Layout.MeasurePointLabel)?.InnerText.Trim()
                             ?? string.Empty,
                RateGPerHour = rate
            };
            page.SpaceTimeMin = ReadSpaceTime(table);

            for (int s = 0; s < Gb21655Layout.SamplesPerTable; s++)
            {
                string header = Row(table, Gb21655Layout.HeaderRow)?
                    .Elements<TableCell>().ElementAtOrDefault(s)?.InnerText.Trim() ?? string.Empty;
                if (header.Length == 0) continue;   // 本页不足 3 个样品 → 尾部空槽不产生工位

                var st = new DryingRateStationRowModel
                {
                    Station = ParseFirstInt(header) ?? (s + 1)
                };

                // R1 [m0:][值]: 干布重(g) → mg; 空 → 未参与
                double? m0 = ParseCellNumber(Row(table, Gb21655Layout.M0Row)?
                    .Elements<TableCell>().ElementAtOrDefault(2 * s + 1));
                if (m0 != null)
                {
                    st.ClothWeightMg = Math.Round(m0.Value * 1000.0);
                    st.Participated = true;
                }

                // 时间网格 [时间|Δmi|mi]: Δmi(g)×1000 = 累计蒸发量(mg); 首行 mi(g) 用于还原滴水量。
                // 早停/尾部留空 → 从该点起没有记录, 直接截断曲线(与生成侧 EvapAtMin 的留空一致)。
                var curve = new List<double>();
                double? mi0 = null;
                for (int k = 0; ; k++)
                {
                    var row = Row(table, Gb21655Layout.FirstTimeRow + k);
                    if (row == null) break;
                    double? deltaMg = ParseCellNumber(row.Elements<TableCell>().ElementAtOrDefault(3 * s + 1));
                    if (deltaMg == null) break;
                    curve.Add(Math.Round(deltaMg.Value * 1000.0));
                    if (k == 0)
                        mi0 = ParseCellNumber(row.Elements<TableCell>().ElementAtOrDefault(3 * s + 2));
                }

                if (st.Participated)
                {
                    if (curve.Count == 0 || mi0 == null)
                        throw new InvalidOperationException($"样品{st.Station} 缺少蒸发曲线/布样总重, 无法还原滴水量");
                    st.EvaporationCurveMg = curve;
                    st.WaterMg = Math.Round(mi0.Value * 1000.0) - st.ClothWeightMg;
                }

                page.Stations.Add(st);
            }

            return page;
        }

        /// <summary>结果表判据: R0 表头含"样品"(备注表虽然也含"水分蒸发时间", 但没有样品表头)。</summary>
        private static bool HasSampleHeader(Table table)
            => Row(table, Gb21655Layout.HeaderRow)?.InnerText.Contains("样品") == true;

        /// <summary>由时间网格前两行标签差还原采样间隔(分钟): "0 min"/"3 min" → 3。</summary>
        private static int ReadSpaceTime(Table table)
        {
            int? first = ParseFirstInt(Row(table, Gb21655Layout.FirstTimeRow)?
                .Elements<TableCell>().ElementAtOrDefault(0)?.InnerText);
            int? second = ParseFirstInt(Row(table, Gb21655Layout.FirstTimeRow + 1)?
                .Elements<TableCell>().ElementAtOrDefault(0)?.InnerText);
            if (first == null || second == null || second.Value - first.Value <= 0)
                throw new InvalidOperationException("报告结果表的时间标签无法读取采样间隔");
            return second.Value - first.Value;
        }

        /// <summary>在给定元素集合(一页/整篇)的表里按标签格文字找值格; 找不到返回 null。</summary>
        private static TableCell? FindValueCellIn(IEnumerable<OpenXmlElement> elements, string label)
        {
            foreach (var table in elements.OfType<Table>())
            {
                foreach (var row in table.Elements<TableRow>())
                {
                    var cells = row.Elements<TableCell>().ToList();
                    if (cells.Count <= Gb21655Layout.ValueColumn) continue;
                    if (cells[0].InnerText.Trim() == label)
                        return cells[Gb21655Layout.ValueColumn];
                }
            }
            return null;
        }

        /// <summary>读一个数值格(空格/非数字 → null)。</summary>
        private static double? ParseCellNumber(TableCell? cell)
            => double.TryParse(cell?.InnerText.Trim() ?? string.Empty, NumberStyles.Float,
                CultureInfo.InvariantCulture, out double value) ? value : null;

        /// <summary>取文本里第一个数字(干燥速率格是 "1.791 (g/h)" 这种带单位的形态)。</summary>
        private static double? ParseFirstNumber(string? text)
        {
            var m = Regex.Match(text ?? string.Empty, @"-?\d+(\.\d+)?");
            return m.Success && double.TryParse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
                ? v : null;
        }

        /// <summary>取文本里第一个整数("样品4" → 4)。</summary>
        private static int? ParseFirstInt(string? text)
        {
            var m = Regex.Match(text ?? string.Empty, @"-?\d+");
            return m.Success && int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
                ? v : null;
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
        /// 取模板"整页"元素块(整页克隆的克隆源)。模板 body 布局:
        /// [前导分节空段(带 w:pPr/w:sectPr)] 摘要表 干燥速率表 结果表 Equipment 段 备注表 [文末 body 级 sectPr]。
        /// (摘要表内含"测点"行; "干燥速率"行也可能是摘要表后独立的单行表。)
        /// 页块 = 文末 body 级 sectPr 之前、前导分节段之后的全部元素 —— 摘要(报告号/均值速率)/样品名称/
        /// Equipment/备注 全部随页重复。插入位置与分页符由 WordEditEngine.ClonePageBlock 处理,
        /// 这里只负责"哪些元素算一页"。
        /// </summary>
        private static List<OpenXmlElement> CollectTemplatePage(WordprocessingDocument doc)
        {
            var body = doc.MainDocumentPart?.Document?.Body
                ?? throw new InvalidOperationException("GB21655 模板正文缺失");
            var lastSectPr = body.Elements<SectionProperties>().LastOrDefault();

            var page = new List<OpenXmlElement>();
            foreach (var e in body.ChildElements)
            {
                if (ReferenceEquals(e, lastSectPr)) break;
                // 前导分节空段(带 w:pPr/w:sectPr)不属于页内容, 不随页克隆
                if (e is Paragraph p && p.ParagraphProperties?.SectionProperties is not null) continue;
                page.Add(e);
            }
            return page;
        }

        /// <summary>
        /// 填一张结果表的 3 个样品槽:
        ///   R0 表头 → "样品{工位号}"(克隆表改号, 原表重写同文本无害);
        ///   R1 m0 值格(每样品 [m0:][值] 成对, 值在 2s+1) = 干布重(g);
        ///   时间网格(每样品 [时间|Δmi|mi]) → 行数按采样间隔重排(见 ResizeTimeGrid),
        ///   时间标签格写 序号×采样间隔, Δmi = 该点蒸发量, mi = 布样总重;
        ///   未参与工位留空; 曲线没记到那一点(早停)也留空(不凭空填数, 也不插值)。
        ///
        /// 为什么时间标签要由代码写: 模板时间行的 "0 min".."60 min" 是 3 分钟一格的静态文字, 代码
        /// 从不写这格。采样间隔一旦大于 3 分钟, 不重写标签、不裁掉多余尾行, 报告的时间轴就是假的
        /// (每格标着的分钟数和实际采样点对不上)。取值一律落在网格点上(序号×间隔 分钟),
        /// 所以取到的就是该点实测值 —— 不再插值出假数。
        /// </summary>
        private void FillSampleGroup(Table table, List<DryingRateStationRowModel> group, int spaceTimeMin)
        {
            int gridRows = ResizeTimeGrid(table, spaceTimeMin);

            for (int s = 0; s < Gb21655Layout.SamplesPerTable; s++)
            {
                var st = s < group.Count ? group[s] : null;
                bool participated = st?.Participated == true;

                SetCellText(Row(table, Gb21655Layout.HeaderRow)!, s,
                    st != null ? $"样品{st.Station}" : "");

                SetCellText(Row(table, Gb21655Layout.M0Row)!, 2 * s + 1,
                    participated ? (st!.ClothWeightMg / 1000.0).ToString("F3") : "");

                for (int k = 0; k < gridRows; k++)
                {
                    var row = Row(table, Gb21655Layout.FirstTimeRow + k)!;
                    int minutes = k * spaceTimeMin;
                    SetCellText(row, 3 * s + 0, $"{minutes} min");
                    double? evap = participated
                        ? Nf5022Formulas.EvapAtMin(minutes, st!.EvaporationCurveMg, spaceTimeMin)
                        : null;
                    SetCellText(row, 3 * s + 1,
                        evap.HasValue ? (evap.Value / 1000.0).ToString("F3") : "");
                    SetCellText(row, 3 * s + 2,
                        evap.HasValue ? ((st!.ClothWeightMg + st.WaterMg - evap.Value) / 1000.0).ToString("F3") : "");
                }
            }
        }

        /// <summary>
        /// 按采样间隔重排结果表的时间网格行(只删尾部多余行), 返回本次网格行数(含 0 分钟那一行)。
        /// 行数 = ceil(60/间隔)+1 —— 排到 60 分钟, 与 GBT2023 的停止点(间隔×(点数−1) 达到 60)同格;
        /// 间隔不能整除 60 时向上取整(sp=7 → 10 行, 末格 63 min), 保证末个实测点也在表里。
        ///
        /// 模板自带 21 行(3 分钟一格)是行数上限: 间隔等于 3 时行数正好 21 → 一行不删, 写出的标签文本
        /// 与模板静态文字相同 → sp=3 的产物与改动前逐格一致。间隔更大则删掉尾部多余行。
        /// 间隔小于 3 需要多于 21 行, 本方法不克隆行 → 明确抛异常(前端输入框与服务层校验都拦在前面)。
        /// </summary>
        private static int ResizeTimeGrid(Table table, int spaceTimeMin)
        {
            if (spaceTimeMin <= 0)
                throw new InvalidOperationException("GB21655 采样间隔须为正整数分钟");

            int gridRows = (Gb21655Layout.GridMinutes + spaceTimeMin - 1) / spaceTimeMin + 1;
            int templateRows = Gb21655Layout.LastTimeRow - Gb21655Layout.FirstTimeRow + 1;

            if (gridRows > templateRows)
                throw new InvalidOperationException(
                    $"GB21655 采样间隔 {spaceTimeMin} 分钟需要 {gridRows} 行时间网格, " +
                    $"模板只有 {templateRows} 行(最小支持 3 分钟), 不支持按行扩容");

            if (gridRows < templateRows)
                WordEditEngine.RemoveRowsAfter(table, Gb21655Layout.FirstTimeRow + gridRows - 1);

            return gridRows;
        }

        /// <summary>
        /// 页脚: 把环境温度/湿度填进 footer1 签名行末两格(R1 第 3 格 °C、第 4 格 %RH),
        /// 同克重 PHY_Weight 的页脚处理(模板这三份 TÜV 报告的 footer1 结构一致)。
        /// 按 "%RH" 标记定位 footer, 结构不符立即抛异常; 模型值空白 → 不填(保留模板横线)。
        /// </summary>
        private void FillFooter(WordprocessingDocument doc, DryingRateReportFillModel model)
            => FillFooter(doc, model.Temperature, model.Humidity);

        /// <summary>页脚温湿度写入(合并报告只有一套页脚, 取最新一份非空值传入)。</summary>
        private void FillFooter(WordprocessingDocument doc, string temperature, string humidity)
        {
            var footer = doc.MainDocumentPart?.FooterParts
                .FirstOrDefault(fp => fp.Footer?.InnerText.Contains("%RH") == true)
                ?? throw new InvalidOperationException("GB21655 模板缺少页脚温湿度表(含 %RH 标记)");
            var footerEl = footer.Footer
                ?? throw new InvalidOperationException("GB21655 模板页脚温湿度部件缺失");

            var table = footerEl.Elements<Table>().FirstOrDefault()
                ?? throw new InvalidOperationException("GB21655 模板页脚温湿度表缺失表格");

            var row = table.Elements<TableRow>().ElementAtOrDefault(1)
                ?? throw new InvalidOperationException("GB21655 模板页脚温湿度表缺 R1(温湿度)行");
            var cells = row.Elements<TableCell>().ToList();
            if (cells.Count < 4)
                throw new InvalidOperationException("GB21655 模板页脚温湿度表 R1 格数不足(应含温度/湿度格)");

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

        /// <summary>
        /// 校验 GB21655 模板结构并返回已定位的锚点: 摘要表 + 两个值格(样品名称/干燥速率) + 结果表。
        /// 这里把 FillReport 会用到的所有锚点(行存在性、列数、标记文字)逐项断言,
        /// 任何一项不符立即抛异常, 让生成失败暴露在调用处, 而不是把错位文档发出去。
        /// </summary>
        private (Table Summary, TableCell SampleNameCell, TableCell RateCell, Table Result) ValidateTemplate(WordprocessingDocument doc)
        {
            var summary = LocateTable(doc, Gb21655Layout.SummaryTableMarker)
                ?? throw new InvalidOperationException("GB21655 模板缺少摘要表(Test Report Number)");
            if (Row(summary, Gb21655Layout.SummaryRowReportNumber) == null)
                throw new InvalidOperationException("GB21655 模板摘要表缺 R0(报告号) 行");
            if (Row(summary, Gb21655Layout.SummaryRowReportNumber)!.Elements<TableCell>().Count() < 2)
                throw new InvalidOperationException("GB21655 模板摘要表 R0 应有[标签|值]两格");

            var sampleNameCell = FindValueCellByLabel(doc, Gb21655Layout.MeasurePointLabel)
                ?? throw new InvalidOperationException("GB21655 模板缺少样品名称行(测点：)");
            var rateCell = FindValueCellByLabel(doc, Gb21655Layout.RateLabel)
                ?? throw new InvalidOperationException("GB21655 模板缺少干燥速率行(干燥速率：)");

            var result = LocateTable(doc, Gb21655Layout.ResultTableMarker)
                ?? throw new InvalidOperationException("GB21655 模板缺少结果表(水分蒸发时间)");
            if (Row(result, Gb21655Layout.LastTimeRow) == null)
                throw new InvalidOperationException($"GB21655 模板结果表行数不足(缺到 R{Gb21655Layout.LastTimeRow} 行)");
            if (Row(result, Gb21655Layout.HeaderRow)!.Elements<TableCell>().Count() < Gb21655Layout.SamplesPerTable)
                throw new InvalidOperationException("GB21655 模板结果表头格数不足(应含 3 样品)");
            if (Row(result, Gb21655Layout.FirstTimeRow)!.Elements<TableCell>().Count() < Gb21655Layout.CellsPerSample * Gb21655Layout.SamplesPerTable)
                throw new InvalidOperationException($"GB21655 模板结果表网格行格数不足(应 {Gb21655Layout.SamplesPerTable} 样品×{Gb21655Layout.CellsPerSample} 列)");

            return (summary, sampleNameCell, rateCell, result);
        }

        /// <summary>
        /// 按标签格文字定位 [标签|值] 行, 返回值格(第 ValueColumn 格); 找不到返回 null。
        /// 不绑表: 模板改版会把这两行在"摘要表内 / 独立单行表"之间搬(曾搬过一次),
        /// 只认标签文字 —— 落在哪张表里都定位得到。取全文档第一个命中行。
        /// 判据是"首格去空白后与标签全等"而非包含: 摘要表里还有静态格写着"干燥速率"(R3 标准引文行),
        /// 用包含判定一旦那格补上冒号就会静默命中、把均值写进引文行; 全等判定下宁可 ValidateTemplate
        /// 抛"缺少干燥速率行"这种看得见的错, 也不出错位文档。
        /// </summary>
        private static TableCell? FindValueCellByLabel(WordprocessingDocument doc, string label)
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return null;

            foreach (var row in body.Elements<Table>().SelectMany(t => t.Elements<TableRow>()))
            {
                var cells = row.Elements<TableCell>().ToList();
                if (cells.Count <= Gb21655Layout.ValueColumn) continue;
                if (cells[0].InnerText.Trim() == label)
                    return cells[Gb21655Layout.ValueColumn];
            }
            return null;
        }

        /// <summary>
        /// 每页"干燥速率"值格 = 本页参与样品速率的算术平均(6 样品分两页时 1/2/3 一页、4/5/6 一页,
        /// 各页各算各的 —— 不是全报告一个均值)。模板值格原内容 = 单位 token"(g/h)",
        /// SetCellText 整格重建会把它冲掉, 故先取回再拼在数值后。整组无参与样品时该页不会产生, 这里只做空集守卫。
        /// </summary>
        private void FillPageRate(TableCell rateCell, IReadOnlyList<DryingRateStationRowModel> group)
        {
            var participated = group.Where(s => s.Participated).ToList();
            if (participated.Count == 0) return;
            FillRateValue(rateCell, participated.Average(s => s.RateGPerHour));
        }

        /// <summary>
        /// 写"干燥速率："值格: 数值 + 模板原单位 token。模板值格原内容 = "(g/h)",
        /// SetCellText 整格重建会把它冲掉, 故先取回再拼在数值后。
        /// 单份报告传本页均值; 合并报告传历史报告上印的值(原样沿用, 不重算)。
        /// </summary>
        private void FillRateValue(TableCell rateCell, double avgRateGPerHour)
        {
            string unit = rateCell.InnerText.Trim();
            string value = avgRateGPerHour.ToString("F3");
            SetCellText(rateCell, unit.Length > 0 ? $"{value} {unit}" : value, bold: true);
        }

        /// <summary>
        /// 在某一页的速率表里取"干燥速率："行的值格。整页克隆后文档里有多张同构表,
        /// 全局 FindValueCellByLabel 只会返回第一个 —— 按页填均值必须限定到本页这张表。
        /// </summary>
        private static TableCell? FindRateValueCell(Table? rateTable)
        {
            if (rateTable == null) return null;
            foreach (var row in rateTable.Elements<TableRow>())
            {
                var cells = row.Elements<TableCell>().ToList();
                if (cells.Count <= Gb21655Layout.ValueColumn) continue;
                if (cells[0].InnerText.Trim() == Gb21655Layout.RateLabel)
                    return cells[Gb21655Layout.ValueColumn];
            }
            return null;
        }

        /// <summary>把一张曲线 PNG 追加到文档末尾(模板无占位段; 同 AATCC "曲线图放在表最后")。</summary>
        /// <param name="index">第几张样品图 → docPr Id/名字带序号(同一文档多张图 id 不能重复)。</param>
        private static void AppendChartAtEnd(WordprocessingDocument doc, byte[] png, int index)
        {
            var mainPart = doc.MainDocumentPart
                ?? throw new InvalidOperationException("GB21655 文档主部件缺失");
            var document = mainPart.Document
                ?? throw new InvalidOperationException("GB21655 文档主体缺失");
            var body = document.Body
                ?? throw new InvalidOperationException("GB21655 文档正文缺失");

            var imagePart = mainPart.AddImagePart(ImagePartType.Png);
            using (var ms = new MemoryStream(png))
                imagePart.FeedData(ms);
            string relId = mainPart.GetIdOfPart(imagePart);

            var drawing = CreateChartDrawing(relId, $"Gb21655Chart{index}", index,
                Gb21655Layout.ChartWidthEmu, Gb21655Layout.ChartHeightEmu);

            var para = new Paragraph(new Run(new RunProperties(new NoProof()), drawing));
            // body 级 sectPr 必须是 w:body 最后一个孩子(schema 规定); 直接 Append 会把曲线段排到 sectPr 之后
            // → 文档违例。曲线图本就该在文末 → 插到最后一个 body 级 sectPr 之前(无 sectPr 才 Append 兜底)。
            var lastSectPr = body.Elements<SectionProperties>().LastOrDefault();
            if (lastSectPr != null)
                lastSectPr.InsertBeforeSelf(para);
            else
                body.Append(para);
        }

        private static Drawing CreateChartDrawing(string relationshipId, string imageName, int id, long widthEmu, long heightEmu)
        {
            uint uid = (uint)(id + 1);
            return new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                    new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties { Id = uid, Name = imageName },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks { NoChangeAspect = true }
                    ),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties { Id = uid, Name = $"{imageName}.png" },
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

        /// <summary>PHY_GB21655_DryingRate.docx 模板坐标 — 模板布局一变, 只改这里。即交付给实验室的模板契约。</summary>
        private static class Gb21655Layout
        {
            // 定位文本 (LocateTable 按 InnerText.Contains 匹配)
            public const string SummaryTableMarker = "Test Report";       // 表0: 摘要表
            public const string ResultTableMarker = "水分蒸发时间";        // 表2: 结果表(备注表也含此词, 但结果表在前)

            // 按标签文字定位的两行(位置随模板改版搬动, 不绑表):
            //   测点：值格 = 样品名称;  干燥速率：值格 = 均值 + 模板原单位 token
            // 两个标签都带全角冒号且要求首格全等 → 不会误中摘要 R3 的静态行"干燥速率"(无冒号)
            public const string MeasurePointLabel = "测点：";        // [测点：|值]
            public const string RateLabel = "干燥速率：";           // [干燥速率：|(g/h)]

            // 表0 (摘要表): [标签|值], 值在第 1 列; R3 标准引文/R4 加水量/R7 洗前洗后为模板静态内容
            public const int SummaryRowReportNumber = 0;  // R0 报告号
            public const int ValueColumn = 1;             // 值所在列

            // 表2 (结果表)
            public const int HeaderRow = 0;       // R0 样品1/2/3(各 span4)
            public const int M0Row = 1;           // R1 [m0:][值]×3, 值在 2s+1
            public const int FirstTimeRow = 4;    // R4 首个时间行(模板静态 0 min)
            public const int LastTimeRow = 24;    // R24 末个时间行(模板静态 60 min, 3 分钟一格共 21 行)
            public const int SamplesPerTable = 3; // 每表样品数(超 3 克隆整表)
            public const int CellsPerSample = 3;  // 网格每样品列数 [时间|Δmi|mi]

            // 时间网格覆盖的观测窗(分): 时间标签由 FillSampleGroup 按采样间隔排到这一刻。
            // 为什么是 60: GBT2023 的停止条件是 sp×(n−1) 达到 60 分钟, 所以报告网格排到 60
            // 正好与停止点同格(间隔不能整除 60 时取向上整: sp=7 → 末格 63)。
            public const int GridMinutes = 60;

            // 曲线图显示尺寸(EMU, 1cm=360000): 宽 12cm, 高按图像素 1400:800=7:4 等比 → ≈6.9cm
            // (整体略缩自 14cm × 8cm; 保持同比例不拉伸)
            public const long ChartWidthEmu = 12 * 360000L;        // 4,320,000 EMU = 12cm
            public const long ChartHeightEmu = 12 * 360000L * 4 / 7;  // ≈ 2,468,571 EMU ≈ 6.86cm
        }

        private static TableRow? Row(Table? t, int i) => t?.Elements<TableRow>().ElementAtOrDefault(i);

        /// <summary>
        /// 按坐标写单元格文本(0-based), 保留原样式。空文本清空该格。
        /// </summary>
        private void SetCellText(TableRow row, int cellIndex, string text, bool bold = false, int? fontSizeHalfPoints = null)
            => SetCellText(row.Elements<TableCell>().ElementAtOrDefault(cellIndex), text, bold, fontSizeHalfPoints);

        /// <summary>
        /// 写单元格文本, 保留原样式。空文本清空该格。
        ///
        /// 流程: ①先抓取原样式(RunProperties) → ②删除多余段落只留首段 → ③删光该段所有 run → ④按新文本重建 run。
        /// 为什么要"先抓样式再删内容": 新 run 的样式(字号/字体/加粗)必须从旧 run 复制;
        /// 而旧 run 在步骤③会被删掉, 所以顺序反了就再也取不到样式源, 填进去的字会变成默认格式。
        /// 样式源抓取带兜底: 优先带 rPr 的 run, 其次任意 run(模板值格可能只有裸 run),
        /// 最后段落的段标记 rPr(pPr/rPr)。必须先取——删除多余段落后就再也取不到样式源了。
        /// bold/fontSizeHalfPoints = 在该样式源之上再叠加加粗/字号(报告号、干燥速率均值要一眼可见)。
        /// </summary>
        private void SetCellText(TableCell? cell, string text, bool bold = false, int? fontSizeHalfPoints = null)
        {
            if (cell == null) return;

            // 样式源第三档(段标记 rPr)是 GB21655 新模板的命门: 结果表网格整片留空(一个 run 都没有),
            // 只有段标记带着 rFonts(黑体/Arial) + sz20 —— 拿不到它, 填进去的时间/蒸发量就会
            // 掉回文档默认字体, 与模板清空前的外观不一致(值是"黑体 10pt", 标签却成了默认字体)。
            var refRun = cell.Descendants<Run>().FirstOrDefault(r => r.RunProperties != null)
                         ?? cell.Descendants<Run>().FirstOrDefault();
            var rp = refRun?.RunProperties?.CloneNode(true) as RunProperties
                     ?? CopyParagraphMarkRunProperties(cell.Elements<Paragraph>().FirstOrDefault()
                            ?.ParagraphProperties?.GetFirstChild<ParagraphMarkRunProperties>())
                     ?? new RunProperties();

            if (bold) WordEditEngine.MakeBold(rp);
            if (fontSizeHalfPoints != null) WordEditEngine.SetFontSize(rp, fontSizeHalfPoints.Value);

            // 保留第一个段落, 删除多余段落
            var paragraphs = cell.Elements<Paragraph>().ToList();
            for (int i = 1; i < paragraphs.Count; i++) paragraphs[i].Remove();
            var para = paragraphs.FirstOrDefault();
            if (para == null) { para = new Paragraph(); cell.Append(para); }

            foreach (var run in para.Elements<Run>().ToList()) run.Remove();
            if (string.IsNullOrEmpty(text)) return;

            var newRun = new Run(rp);
            para.Append(newRun);
            TextRunHelper.InsertTextWithLineBreaks(text, newRun);
        }

        /// <summary>
        /// 把段落的段标记 rPr（pPr/rPr）搬成 run 的 RunProperties。
        ///
        /// 为什么要搬而不是直接克隆: 两者标签都是 w:rPr 但 CLR 类型不同,
        /// 段标记那个是 ParagraphMarkRunProperties(不是 RunProperties 的子类), 只能逐个搬子元素。
        /// 子元素顺序照抄源顺序 —— pPr/rPr 本身已按 schema 的 rPr 子元素序列排好, 搬完仍然合法。
        /// </summary>
        private static RunProperties? CopyParagraphMarkRunProperties(ParagraphMarkRunProperties? mark)
        {
            if (mark == null) return null;
            var rp = new RunProperties();
            foreach (var child in mark.ChildElements)
                rp.Append(child.CloneNode(true));
            return rp;
        }

        /// <summary>
        /// 定位表格（支持书签、内容匹配、索引等多种策略）。
        ///
        /// 策略优先级: 书签 > 表格内文字 > 表格序号。
        ///   - 书签: 模板里显式加了书签标记时最稳(文字改动不影响);
        ///   - 内容: 按表格里是否包含某段文字找——本引擎默认用这个, 模板里表头文字变了
        ///     就定位不到, 会返回 null 再被 ValidateTemplate 抛异常兜住;
        ///   - 索引: 按文档第几张表(0-based), 最脆弱, 模板增删表就错位, 仅作兜底。
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
