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
    /// "干燥速率："行值格 = 参与工位回归斜率(g/h) 的均值。
    /// 注意"测点："与"干燥速率："两行的**位置会被实验室改动**(2026-09-11 两行互换:
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
        /// 摘要(报告号/干燥速率均值)/样品名称/Equipment/备注随页重复。
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

            // 参与工位按 3 个一组切页; 整组无参与工位 → 该组不出页。
            //   第 1 组直接用模板自带整页; 之后每组整页克隆。克隆时机在填网格之前:
            //   上面摘要/样品名/速率已填(克隆页继承同款报告头), 结果网格此刻仍空 → 克隆不会把上一组数据带进新页。
            var groups = new List<List<DryingRateStationRowModel>>();
            for (int start = 0; start < model.Stations.Count; start += Gb21655Layout.SamplesPerTable)
            {
                var group = model.Stations.Skip(start).Take(Gb21655Layout.SamplesPerTable).ToList();
                if (group.Any(s => s.Participated)) groups.Add(group);
            }

            var pageBlock = CollectTemplatePage(doc);        // 模板整页元素块(克隆源)
            var pageTables = new List<Table> { result };     // 每组一页: [0]=模板自带页
            for (int gi = 1; gi < groups.Count; gi++)
            {
                var clones = WordEditEngine.ClonePageBlock(body, pageBlock);   // 分页符段+整页深拷贝 → 文末 sectPr 前
                Table? pageResult = null;
                for (int i = 0; i < pageBlock.Count; i++)
                    if (ReferenceEquals(pageBlock[i], result)) pageResult = clones[i] as Table;
                if (pageResult == null)
                    throw new InvalidOperationException("GB21655 模板页块缺结果表, 无法克隆新页");
                pageTables.Add(pageResult);
            }

            // 每页各填自己的 3 个样品槽(表头样品号=工位号、m0、0..60min 网格; 未参与留空)
            for (int gi = 0; gi < groups.Count; gi++)
                FillSampleGroup(pageTables[gi], groups[gi], model.SpaceTimeMin);

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
        /// 取模板"整页"元素块(整页克隆的克隆源)。模板 body 布局:
        /// [前导分节空段(带 w:pPr/w:sectPr)] 摘要表 干燥速率表 结果表 Equipment 段 备注表 [文末 body 级 sectPr]。
        /// (摘要表内含"测点"行; 2026-09-11 版把"干燥速率"行抽成摘要表后独立的单行表。)
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
        ///   R4..R24 网格(每样品 [时间|Δmi|mi], Δmi=蒸发量, mi=布样总重) = 回归网格取值器按分钟插值;
        ///   未参与工位留空; 超出曲线记录范围的分钟也留空(不凭空填数)。
        /// </summary>
        private void FillSampleGroup(Table table, List<DryingRateStationRowModel> group, int spaceTimeMin)
        {
            for (int s = 0; s < Gb21655Layout.SamplesPerTable; s++)
            {
                var st = s < group.Count ? group[s] : null;
                bool participated = st?.Participated == true;

                SetCellText(Row(table, Gb21655Layout.HeaderRow)!, s,
                    st != null ? $"样品{st.Station}" : "");

                SetCellText(Row(table, Gb21655Layout.M0Row)!, 2 * s + 1,
                    participated ? (st!.ClothWeightMg / 1000.0).ToString("F3") : "");

                for (int r = Gb21655Layout.FirstTimeRow; r <= Gb21655Layout.LastTimeRow; r++)
                {
                    int t = (r - Gb21655Layout.FirstTimeRow) * Gb21655Layout.TimeStepMin;
                    double? evap = participated
                        ? Nf5022Formulas.EvapAtMin(t, st!.EvaporationCurveMg, spaceTimeMin)
                        : null;
                    SetCellText(Row(table, r)!, 3 * s + 1,
                        evap.HasValue ? (evap.Value / 1000.0).ToString("F3") : "");
                    SetCellText(Row(table, r)!, 3 * s + 2,
                        evap.HasValue ? ((st!.ClothWeightMg + st.WaterMg - evap.Value) / 1000.0).ToString("F3") : "");
                }
            }
        }

        /// <summary>
        /// 页脚: 把环境温度/湿度填进 footer1 签名行末两格(R1 第 3 格 °C、第 4 格 %RH),
        /// 同克重 PHY_Weight 的页脚处理(模板这三份 TÜV 报告的 footer1 结构一致)。
        /// 按 "%RH" 标记定位 footer, 结构不符立即抛异常; 模型值空白 → 不填(保留模板横线)。
        /// </summary>
        private void FillFooter(WordprocessingDocument doc, DryingRateReportFillModel model)
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
        /// 不绑表: 模板改版会把这两行在"摘要表内 / 独立单行表"之间搬(2026-09-11 就搬过一次),
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
            public const int FirstTimeRow = 4;    // R4 0 min
            public const int LastTimeRow = 24;    // R24 60 min
            public const int TimeStepMin = 3;     // 网格步长(分)
            public const int SamplesPerTable = 3; // 每表样品数(超 3 克隆整表)
            public const int CellsPerSample = 3;  // 网格每样品列数 [时间|Δmi|mi]

            // 曲线图显示尺寸(EMU, 1cm=360000): 宽 12cm, 高按图像素 1400:800=7:4 等比 → ≈6.9cm
            // (2026-09-08 目视反馈整体略缩, 原 14cm × 8cm; 保持同比例不拉伸)
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
        /// 样式源抓取带兜底: 优先带 rPr 的 run, 其次任意 run(模板值格可能只有裸 run)。
        /// bold/fontSizeHalfPoints = 在该样式源之上再叠加加粗/字号(报告号、干燥速率均值要一眼可见)。
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

            var newRun = new Run(rp);
            para.Append(newRun);
            TextRunHelper.InsertTextWithLineBreaks(text, newRun);
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
