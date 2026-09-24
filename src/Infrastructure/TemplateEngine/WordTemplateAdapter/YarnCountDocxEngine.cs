using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.YarnCountContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter
{
    /// <summary>
    /// 纱支(Yarn Count) docx 填充引擎 — 按坐标填格, 与克重/耐磨/干燥速率各引擎互相隔离。
    ///
    /// 模板 PHY_YarnCount.docx 正文两张表 + 一个页脚:
    ///   表0(摘要, 按 "Test Report Number" 定位): 报告号、Warp/Weft/Knit (Tex) 三个汇总格;
    ///   表1(数据, 按 "#1" 定位): 10 个长度读数行 + Average(cm) / Mass(g/50) / Tex 三行, 每行 7 格
    ///        —— 格0 是行标签, 格1~6 是数据列: Warp#1 | Warp#2 | Weft#1 | Weft#2 | Knit#1 | Knit#2;
    ///   页脚: 按 "%RH" 标记定位, R1 的 格2=温度 / 格3=湿度(带下划线, 模拟"写在横线上")。
    ///
    /// 方向顺序与各方向列数只有**一处**事实来源: YarnCountDocxLayout.DirectionColumns
    /// (与契约层的 WarpSpecimenCount / WeftSpecimenCount / KnitSpecimenCount 同源)。
    ///
    /// 2026-09 模板改版: 原为 Warp 2 + Weft 5 的刻意不对称, 现为三方向各 2 列。
    /// 注意改版后**方向顺序只能靠数据表 R0 的跨列标题识别** —— R1 的三组表头都是 "#1 #2",
    /// 长得一模一样, 已经把"哪一组属于哪个方向"这条信息丢掉了(见 ValidateDirectionHeaderRow)。
    ///
    /// 所有坐标集中在底部 YarnCountDocxLayout; 配套 ValidateTemplate 做结构校验 ——
    /// 结构一旦与坐标假设不符立即抛异常, 宁可生成失败, 也不产出"能打开、实则整块错位"的报告。
    ///
    /// 引擎**不做业务计算**: 平均/Tex/方向汇总都在 YarnCountReportService 里算好并舍入,
    /// 这里只把已定稿的数字按坐标落格, 并按同样的取位格式化成文本。
    /// </summary>
    public class YarnCountDocxEngine : IYarnCountDocxEngine, IScopedDependency
    {
        /// <summary>
        /// 报告号字号(半磅): 28 = 14pt。
        /// 为什么必须显式给: 模板该值格是空的(一个 run 都没有), 取不到样式源 → 只能吃文档默认(10pt),
        /// 反而比左侧 "Test Report Number:" 标签还小。14pt 让它压过标签、一眼可见。
        /// </summary>
        private const int ReportNumberFontSizeHalfPoints = 28;

        /// <summary>
        /// 填充纱支报告 — 流程地图:
        ///   1. 打开文件, 定位表0与表1并做结构校验(结构不符 → 抛异常);
        ///   2. 表0: 填报告号(加粗放大) + 三个 Tex 汇总格(空值跳过留空);
        ///      "Fabric:" 格(R3)**刻意不动** —— 该格是标准清单, 本流程不产出对应信息;
        ///   3. 表1: 逐试样列填 10 个长度读数 + Average / Mass / Tex(空值跳过, 不写 0);
        ///   4. 页脚: 填温湿度;
        ///   5. 保存。
        /// </summary>
        public void FillReport(string filePath, YarnCountReportFillModel model)
        {
            using var doc = WordprocessingDocument.Open(filePath, true);
            // 结构不符 → 抛异常, 不再静默空白
            var (t0, t1) = ValidateTemplate(doc);

            // ---- 表0: 报告号 ----
            SetCellText(Row(t0, YarnCountDocxLayout.SummaryRowReportNumber), YarnCountDocxLayout.ValueColumn,
                model.ReportNumber, bold: true, fontSizeHalfPoints: ReportNumberFontSizeHalfPoints);

            // ---- 表0: 三个 Tex 汇总格(样式源取同行的标签格) ----
            SetCellTextSeeded(Row(t0, YarnCountDocxLayout.SummaryRowWarpTex),
                YarnCountDocxLayout.ValueColumn, YarnCountDocxLayout.LabelColumn, FormatTex(model.WarpTex));
            SetCellTextSeeded(Row(t0, YarnCountDocxLayout.SummaryRowWeftTex),
                YarnCountDocxLayout.ValueColumn, YarnCountDocxLayout.LabelColumn, FormatTex(model.WeftTex));
            SetCellTextSeeded(Row(t0, YarnCountDocxLayout.SummaryRowKnitTex),
                YarnCountDocxLayout.ValueColumn, YarnCountDocxLayout.LabelColumn, FormatTex(model.KnitTex));

            // ---- 表1: 逐试样列 ----
            foreach (var col in model.Columns)
            {
                int column = YarnCountDocxLayout.ColumnOf(col.Direction, col.SpecimenIndex);

                // 长度读数: 第 i 个读数 → 第 (LengthRowStart + i) 行。
                // 空槽位跳过(模板该格本就是空的), 不写成 0 —— 留空表示"没测", 0 会被读成"测出来是 0"。
                for (int i = 0; i < col.Lengths.Count && i < YarnCountDocxLayout.LengthReadingCount; i++)
                {
                    var value = col.Lengths[i];
                    if (!value.HasValue) continue;
                    SetCellTextSeeded(Row(t1, YarnCountDocxLayout.LengthRowStart + i), column,
                        YarnCountDocxLayout.LabelColumn, Format(value.Value, YarnCountDocxLayout.LengthDecimals));
                }

                SetCellTextSeeded(Row(t1, YarnCountDocxLayout.AverageRow), column,
                    YarnCountDocxLayout.LabelColumn,
                    col.Average.HasValue ? Format(col.Average.Value, YarnCountDocxLayout.AverageDecimals) : "");
                SetCellTextSeeded(Row(t1, YarnCountDocxLayout.MassRow), column,
                    YarnCountDocxLayout.LabelColumn,
                    col.Mass.HasValue ? Format(col.Mass.Value, YarnCountDocxLayout.MassDecimals) : "");
                SetCellTextSeeded(Row(t1, YarnCountDocxLayout.TexRow), column,
                    YarnCountDocxLayout.LabelColumn,
                    col.Tex.HasValue ? Format(col.Tex.Value, YarnCountDocxLayout.TexDecimals) : "");
            }

            // ---- 页脚: 温湿度 ----
            FillFooter(doc, model);

            doc.MainDocumentPart?.Document?.Save();
        }

        /// <summary>Tex 汇总格的文本: 空值返回空串(该格留空, 不是写 0)</summary>
        private static string FormatTex(decimal? value)
            => value.HasValue ? Format(value.Value, YarnCountDocxLayout.TexDecimals) : "";

        /// <summary>
        /// 定长小数字符串。显式 InvariantCulture: 报告是给人看的固定格式,
        /// 不能随服务器区域设置变成 "12,34"(逗号小数点)。
        /// </summary>
        private static string Format(decimal value, int decimals)
            => value.ToString("F" + decimals, CultureInfo.InvariantCulture);

        /// <summary>
        /// 填写页脚温湿度格子(与克重引擎同款格式: 数值居中原横线并加下划线, 两侧 _ 补齐, 后缀跟在行尾)。
        /// 按 "%RH" 标记定位 footer —— 本模板的 %RH 只在 footer1.xml 里, 另两个 footer 没有,
        /// 所以这个内容匹配是唯一命中的; 模板结构不符立即抛异常。
        /// </summary>
        private void FillFooter(WordprocessingDocument doc, YarnCountReportFillModel model)
        {
            var footer = doc.MainDocumentPart?.FooterParts
                .FirstOrDefault(fp => fp.Footer?.InnerText.Contains("%RH") == true)
                ?? throw new InvalidOperationException("PHY_YarnCount 模板缺少页脚温湿度表(含 %RH 标记)");
            var footerEl = footer.Footer
                ?? throw new InvalidOperationException("PHY_YarnCount 模板页脚温湿度部件缺失");

            var table = footerEl.Elements<Table>().FirstOrDefault()
                ?? throw new InvalidOperationException("PHY_YarnCount 模板页脚温湿度表缺失表格");

            var row = table.Elements<TableRow>().ElementAtOrDefault(YarnCountDocxLayout.FooterValueRow)
                ?? throw new InvalidOperationException("PHY_YarnCount 模板页脚温湿度表缺 R1(温湿度)行");
            var cells = row.Elements<TableCell>().ToList();
            if (cells.Count <= YarnCountDocxLayout.FooterHumidityCell)
                throw new InvalidOperationException("PHY_YarnCount 模板页脚温湿度表 R1 格数不足(应含温度/湿度格)");

            if (model.EnvironmentTemperature.HasValue)
                SetFooterValue(cells[YarnCountDocxLayout.FooterTemperatureCell],
                    model.EnvironmentTemperature.Value.ToString("F1", CultureInfo.InvariantCulture), "°C");
            if (model.EnvironmentHumidity.HasValue)
                SetFooterValue(cells[YarnCountDocxLayout.FooterHumidityCell],
                    model.EnvironmentHumidity.Value.ToString("F1", CultureInfo.InvariantCulture), "%RH");

            footerEl.Save();
        }

        /// <summary>
        /// 校验模板结构并返回已定位的表0(摘要)与表1(数据)。
        ///
        /// 为什么宁可抛异常也不静默: 本引擎的填格依赖 YarnCountDocxLayout 里人工数的坐标。
        /// 模板只要被人改过(增删一行/一列/改表头文字), 坐标就可能整体错位 —— 那种情况下继续填,
        /// 会产出"长度读数填进 Average 行、纬向数据写进经向列"这种表面上能打开、实则全错的 docx, 最难以发现。
        ///
        /// 四类断言, 缺一不可:
        ///   ① 行**存在性**(缺行 → 后面 ElementAtOrDefault 静默 null);
        ///   ② 行**标签文字**逐行核对 —— 只查"行存在"是发现不了"中间插图了一行"的, 而那正是最常见的误改;
        ///   ③ 格**数量** —— SetCellText 遇到不存在的格是静默 return, 少一格就是一整列数据凭空消失;
        ///   ④ 表1 **R0 的方向顺序与跨列数** —— 方向只写在 R0 里, 是"哪一列属于哪个方向"的唯一依据。
        /// </summary>
        private (Table Summary, Table Data) ValidateTemplate(WordprocessingDocument doc)
        {
            var t0 = LocateTable(doc, YarnCountDocxLayout.SummaryTableMarker)
                ?? throw new InvalidOperationException("PHY_YarnCount 模板缺少摘要表(Test Report Number)");
            // 行存在性: 最后一个要写的行在, 说明表没被截短
            if (Row(t0, YarnCountDocxLayout.SummaryRowKnitTex) == null)
                throw new InvalidOperationException("PHY_YarnCount 模板摘要表行数不足(缺 R9 Knit (Tex) 行)");

            // 行标签 + 格数: 4 个要写的行逐个核对
            ValidateSummaryRow(t0, YarnCountDocxLayout.SummaryRowReportNumber, "Test Report Number");
            ValidateSummaryRow(t0, YarnCountDocxLayout.SummaryRowWarpTex, "Warp");
            ValidateSummaryRow(t0, YarnCountDocxLayout.SummaryRowWeftTex, "Weft");
            ValidateSummaryRow(t0, YarnCountDocxLayout.SummaryRowKnitTex, "Knit");

            var t1 = LocateTable(doc, YarnCountDocxLayout.DataTableMarker)
                ?? throw new InvalidOperationException("PHY_YarnCount 模板缺少数据表(#1 #2 那行)");

            // R0 方向行: 先钉死"哪一列属于哪个方向"(R1 三组表头一样, 已经分不出来)
            ValidateDirectionHeaderRow(t1);

            // 表头行: 逐格核对 "Length:" + 各方向的 "#1..#N"
            var header = Row(t1, YarnCountDocxLayout.DataHeaderRow)
                ?? throw new InvalidOperationException($"PHY_YarnCount 模板数据表行数不足(缺 R{YarnCountDocxLayout.DataHeaderRow} 表头行)");
            var headerCells = header.Elements<TableCell>().ToList();
            if (headerCells.Count <= YarnCountDocxLayout.MaxDataColumn)
                throw new InvalidOperationException(
                    $"PHY_YarnCount 模板数据表头格数不足(应含 {YarnCountDocxLayout.MaxDataColumn + 1} 格)");
            for (int c = 0; c < YarnCountDocxLayout.DataHeaderLabels.Length; c++)
            {
                var expected = YarnCountDocxLayout.DataHeaderLabels[c];
                var actual = headerCells[c].InnerText.Trim();
                if (actual != expected)
                    throw new InvalidOperationException(
                        $"PHY_YarnCount 模板数据表头第 {c} 格应为「{expected}」, 实际为「{actual}」—— 列序已变, 停止填充");
            }

            // 长度行: 逐个核对行标签开头的序号 == 1..10, 并确保每行都够 MaxDataColumn+1 格
            for (int i = 0; i < YarnCountDocxLayout.LengthReadingCount; i++)
            {
                int rowIndex = YarnCountDocxLayout.LengthRowStart + i;
                ValidateLengthRowLabel(Row(t1, rowIndex), rowIndex, i + 1);
            }

            // Average / Mass / Tex 三行 —— 标签带单位后缀("Average(cm)" / "Mass(g/50)"), 用包含匹配
            ValidateDataRowLabel(Row(t1, YarnCountDocxLayout.AverageRow), YarnCountDocxLayout.AverageRow, "Average");
            ValidateDataRowLabel(Row(t1, YarnCountDocxLayout.MassRow), YarnCountDocxLayout.MassRow, "Mass");
            ValidateDataRowLabel(Row(t1, YarnCountDocxLayout.TexRow), YarnCountDocxLayout.TexRow, "Tex");

            return (t0, t1);
        }

        /// <summary>摘要表某个待写行: 标签文字必须含 marker, 且格数够写到 ValueColumn</summary>
        private static void ValidateSummaryRow(Table t0, int rowIndex, string marker)
        {
            var row = Row(t0, rowIndex)
                ?? throw new InvalidOperationException($"PHY_YarnCount 模板摘要表行数不足(缺 R{rowIndex} 行)");
            if (!row.InnerText.Contains(marker))
                throw new InvalidOperationException(
                    $"PHY_YarnCount 模板摘要表 R{rowIndex} 应含「{marker}」, 实际为「{row.InnerText.Trim()}」—— 行序已变, 停止填充");
            if (row.Elements<TableCell>().Count() <= YarnCountDocxLayout.ValueColumn)
                throw new InvalidOperationException(
                    $"PHY_YarnCount 模板摘要表 R{rowIndex} 格数不足(需能写第 {YarnCountDocxLayout.ValueColumn} 格)");
        }

        /// <summary>
        /// 校验数据表 R0(方向行): 首格为空, 其后每组的文字与**跨列数**必须与
        /// YarnCountDocxLayout.DirectionColumns 按序一致。
        ///
        /// 为什么非要查 R0: 2026-09 改版后 R1 是 "#1 #2 | #1 #2 | #1 #2", 三组长得一模一样,
        /// R1 自己已经分不出 Warp|Weft|Knit 还是 Knit|Weft|Warp —— **方向顺序只写在 R0 里**。
        /// 不查 R0 的话, 把模板的 Weft 与 Knit 两组对调(各组列数不变)将完全不被发现,
        /// 报告会把纬向数据印进针织列, 正是本引擎最怕的"能打开、数字全错"。
        /// 顺带: 跨列数与列数比对, 把"SpecimenCount 常量写错但自洽"这类错也挡住了。
        /// </summary>
        private static void ValidateDirectionHeaderRow(Table t1)
        {
            int rowIndex = YarnCountDocxLayout.DirectionHeaderRow;
            var row = Row(t1, rowIndex)
                ?? throw new InvalidOperationException($"PHY_YarnCount 模板数据表行数不足(缺 R{rowIndex} 方向行)");
            var cells = row.Elements<TableCell>().ToList();
            var expected = YarnCountDocxLayout.DirectionColumns;

            if (cells.Count != expected.Length + 1)
                throw new InvalidOperationException(
                    $"PHY_YarnCount 模板数据表 R{rowIndex} 应有 {expected.Length + 1} 格" +
                    $"(1 个空角格 + {expected.Length} 个方向), 实际 {cells.Count} 格");

            if (!string.IsNullOrWhiteSpace(cells[0].InnerText))
                throw new InvalidOperationException(
                    $"PHY_YarnCount 模板数据表 R{rowIndex} 首格应为空, 实际为「{cells[0].InnerText.Trim()}」");

            for (int i = 0; i < expected.Length; i++)
            {
                var (direction, count) = expected[i];
                var cell = cells[i + 1];

                var actual = cell.InnerText.Trim();
                if (actual != direction)
                    throw new InvalidOperationException(
                        $"PHY_YarnCount 模板数据表方向行第 {i + 1} 组应为「{direction}」, 实际为「{actual}」" +
                        " —— 方向顺序已变, 停止填充");

                int span = cell.TableCellProperties?.GridSpan?.Val?.Value ?? 1;
                if (span != count)
                    throw new InvalidOperationException(
                        $"PHY_YarnCount 模板数据表「{direction}」组跨 {span} 列, 但程序认为它有 {count} 列" +
                        " —— 列数已变, 停止填充");
            }
        }

        /// <summary>
        /// 取数据表某行的标签文字(格0), 顺带断言行存在、格数够写到 MaxDataColumn。
        /// 格数必须每行都查: SetCellText 遇到不存在的格是静默 return, 少一格就是一整列数据凭空消失。
        /// </summary>
        private static string RowLabelOrThrow(TableRow? row, int rowIndex)
        {
            if (row == null)
                throw new InvalidOperationException($"PHY_YarnCount 模板数据表行数不足(缺 R{rowIndex} 行)");

            var cells = row.Elements<TableCell>().ToList();
            if (cells.Count <= YarnCountDocxLayout.MaxDataColumn)
                throw new InvalidOperationException(
                    $"PHY_YarnCount 模板数据表 R{rowIndex} 格数不足(需能写第 {YarnCountDocxLayout.MaxDataColumn} 格)");

            return cells.Count > YarnCountDocxLayout.LabelColumn
                ? cells[YarnCountDocxLayout.LabelColumn].InnerText.Trim()
                : "";
        }

        /// <summary>
        /// 带单位后缀的三行(Average(cm) / Mass(g/50) / Tex): 标签**包含** label 即可 ——
        /// 单位怎么写不该影响坐标, 而这三行的名称彼此不互为子串, 包含匹配不会误放行。
        /// </summary>
        private static void ValidateDataRowLabel(TableRow? row, int rowIndex, string label)
        {
            var actual = RowLabelOrThrow(row, rowIndex);
            if (!actual.Contains(label))
                throw new InvalidOperationException(
                    $"PHY_YarnCount 模板数据表 R{rowIndex} 行标签应为「{label}」, 实际为「{actual}」—— 行序已变, 停止填充");
        }

        /// <summary>
        /// 长度行: 标签**开头连续的 ASCII 数字**必须等于该行序号。
        ///
        /// 为什么不再精确相等: 2026-09 模板给长度行加了单位后缀("1." → "1. (cm)"),
        /// 精确相等会全线失败; 而单位从 (cm) 改成 (mm) 本来也不该让坐标失效。
        /// 为什么不用包含匹配: "1." 是 "10." 的子串, 包含匹配会把错位一行整个放过去。
        /// 取开头数字两头都占: 对后缀免疫, 又拦得住插行/删行/改号(配合 RowLabelOrThrow 的格数校验)。
        /// </summary>
        private static void ValidateLengthRowLabel(TableRow? row, int rowIndex, int expectedNumber)
        {
            var actual = RowLabelOrThrow(row, rowIndex);
            if (LeadingNumber(actual) != expectedNumber)
                throw new InvalidOperationException(
                    $"PHY_YarnCount 模板数据表 R{rowIndex} 行标签应以「{expectedNumber}.」开头, " +
                    $"实际为「{actual}」—— 行序已变, 停止填充");
        }

        /// <summary>取字符串开头连续的 ASCII 数字; 没有数字则返回 -1</summary>
        private static int LeadingNumber(string text)
        {
            int i = 0;
            while (i < text.Length && text[i] >= '0' && text[i] <= '9') i++;
            if (i == 0) return -1;

            return int.TryParse(text.AsSpan(0, i), NumberStyles.None, CultureInfo.InvariantCulture, out int n)
                ? n
                : -1;
        }

        /// <summary>
        /// 纱支模板坐标 — 模板布局一变, 只改这里。
        /// 数字全部是照着 PHY_YarnCount.docx 的 XML 逐格数出来的(0-based)。
        /// </summary>
        private static class YarnCountDocxLayout
        {
            // ---------- 取位(与 YarnCountReportRequestDto 的常量同源, 保证落格与显示一致) ----------
            public static readonly int LengthDecimals = YarnCountReportRequestDto.LengthDecimals;
            public static readonly int AverageDecimals = YarnCountReportRequestDto.AverageDecimals;
            public static readonly int MassDecimals = YarnCountReportRequestDto.MassDecimals;
            public static readonly int TexDecimals = YarnCountReportRequestDto.TexDecimals;
            public static readonly int LengthReadingCount = YarnCountReportRequestDto.LengthReadingCount;

            // ---------- 表0: 摘要表(按 "Test Report Number" 定位) ----------
            public const string SummaryTableMarker = "Test Report Number";

            /// <summary>行标签格恒为 0、值写进第 1 格(报告号 / 三个 Tex 汇总格都是这个形状)</summary>
            public const int LabelColumn = 0;
            public const int ValueColumn = 1;

            public const int SummaryRowReportNumber = 0;
            public const int SummaryRowWarpTex = 6;
            public const int SummaryRowWeftTex = 7;
            public const int SummaryRowKnitTex = 9;
            // 注: R3 是 "Fabric:" | 标准清单 —— 刻意不填, 所以这里没有它的坐标

            // ---------- 表1: 数据表(按 "#1" 定位) ----------
            /// <summary>
            /// 用 "#1" 而不是 "Warp": 表0 不含 "#1", 而 "Warp" 在两处都出现(表0 的 R6 标签、
            /// 表1 的 R0 跨列标题), 用 Warp 定位会先命中表0。
            /// </summary>
            public const string DataTableMarker = "#1";

            public const int DataHeaderRow = 1;

            /// <summary>方向行 —— 每组方向一个跨列标题格, 是本引擎识别"哪一列属于哪个方向"的唯一依据</summary>
            public const int DirectionHeaderRow = 0;

            /// <summary>
            /// 数据列的方向顺序与各方向列数 —— 与模板 R0 的三组跨列标题(Warp | Weft | Knit)**按序**对应。
            ///
            /// 这是本引擎唯一的"方向 → 列"事实来源: 表头期望文字、列偏移、最右列号全部由它推导。
            /// 调整方向或列数只改这里(连同契约层的三个 SpecimenCount 常量), 不再有第二处手写坐标 ——
            /// 之前"手写表头数组 + 逐个算列起点"的写法, 漏改一处不会报错, 只会静默错列。
            /// </summary>
            public static readonly (string Direction, int Count)[] DirectionColumns =
            {
                (YarnCountReportRequestDto.DirectionWarp, YarnCountReportRequestDto.WarpSpecimenCount),
                (YarnCountReportRequestDto.DirectionWeft, YarnCountReportRequestDto.WeftSpecimenCount),
                (YarnCountReportRequestDto.DirectionKnit, YarnCountReportRequestDto.KnitSpecimenCount),
            };

            /// <summary>
            /// 数据列从第几格开始(格0 恒为行标签)
            /// </summary>
            public const int FirstDataColumn = 1;

            /// <summary>
            /// 表头每格的期望文字: "Length:" + 各方向的 "#1..#N", 由 DirectionColumns 生成。
            /// 为什么可以生成而不是手写: 这份数组只编码"数字长什么样", 从来编码不了"哪一组属于哪个方向" ——
            /// 方向顺序由 R0 校验负责(见 ValidateDirectionHeaderRow), 两者合起来才钉死列序。
            /// </summary>
            public static readonly string[] DataHeaderLabels = BuildDataHeaderLabels();

            private static string[] BuildDataHeaderLabels()
            {
                var labels = new List<string> { "Length:" };
                foreach (var (_, count) in DirectionColumns)
                    for (int i = 1; i <= count; i++)
                        labels.Add("#" + i);

                return labels.ToArray();
            }

            public const int LengthRowStart = 2;   // "1. (cm)" ~ "10. (cm)" → R2..R11
            public const int AverageRow = 12;
            public const int MassRow = 13;
            public const int TexRow = 14;

            /// <summary>数据列的最大列号(Tex 行要写到的最右一格) —— 各方向列数之和</summary>
            public static readonly int MaxDataColumn =
                FirstDataColumn + DirectionColumns.Sum(d => d.Count) - 1;

            /// <summary>
            /// 试样号(1-based) → 模板数据列号。
            ///
            /// 服务端已做过方向白名单校验, 这里查不到**必须抛异常, 绝不能留 else 兜底**:
            /// 上一版是 `direction == Warp ? 经向起点 : 纬向起点`, 方向只要不是 Warp 就掉进 else,
            /// 于是 "Knit" 会被**静默**写进 Weft 的列 —— 报告能打开、数字全错, 最难发现的那种。
            /// </summary>
            public static int ColumnOf(string direction, int specimenIndex)
            {
                int offset = FirstDataColumn;
                foreach (var (dir, count) in DirectionColumns)
                {
                    if (dir == direction) return offset + (specimenIndex - 1);
                    offset += count;
                }

                throw new InvalidOperationException(
                    $"纱支数据表没有方向「{direction}」对应的列 —— 停止填充" +
                    $"(合法方向: {string.Join(" / ", DirectionColumns.Select(d => d.Direction))})");
            }

            // ---------- 页脚(按 "%RH" 定位) ----------
            public const int FooterValueRow = 1;
            public const int FooterTemperatureCell = 2;
            public const int FooterHumidityCell = 3;
        }

        private static TableRow? Row(Table? t, int i) => t?.Elements<TableRow>().ElementAtOrDefault(i);

        /// <summary>按坐标写单元格文本(0-based), 保留原样式。空文本清空该格。行/格不存在则静默不动。</summary>
        private void SetCellText(TableRow? row, int cellIndex, string text, bool bold = false, int? fontSizeHalfPoints = null)
            => SetCellText(row?.Elements<TableCell>().ElementAtOrDefault(cellIndex), text, bold, fontSizeHalfPoints);

        /// <summary>
        /// 写单元格文本, 保留原样式。空文本清空该格。
        ///
        /// 流程: ①先抓取原样式(RunProperties) → ②删除多余段落只留首段 → ③删光该段所有 run → ④按新文本重建 run。
        /// 为什么要"先抓样式再删内容": 新 run 的样式(字号/字体/加粗)必须从旧 run 复制;
        /// 而旧 run 在步骤③会被删掉, 所以顺序反了就再也取不到样式源, 填进去的字会变成默认格式。
        /// </summary>
        private void SetCellText(TableCell? cell, string text, bool bold = false, int? fontSizeHalfPoints = null)
        {
            if (cell == null) return;

            var refRun = cell.Descendants<Run>().FirstOrDefault(r => r.RunProperties != null);
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
        /// 写单元格文字; 目标格若没有任何带样式 run(模板空白格), 先从同行样式源格(通常取格0 的行标签)
        /// 克隆 RunProperties 注入, 再走 SetCellText, 保证新写的数字与整行同字体字号。
        ///
        /// 为什么需要: 本模板所有待填格(报告号格、三个 Tex 汇总格、表1 的格1~6)都是空的、一个 run 都没有,
        /// 直接 SetCellText 只能拿到 new RunProperties() → 吃文档默认字体, 与旁边的标签不一致, 报告看着"串版"。
        /// </summary>
        private void SetCellTextSeeded(TableRow? row, int cellIndex, int styleSourceCellIndex, string text)
        {
            var cell = row?.Elements<TableCell>().ElementAtOrDefault(cellIndex);
            if (cell == null) return;

            if (cell.Descendants<Run>().FirstOrDefault(r => r.RunProperties != null) == null)
            {
                var src = row!.Elements<TableCell>().ElementAtOrDefault(styleSourceCellIndex);
                var srcRp = src?.Descendants<Run>().FirstOrDefault(r => r.RunProperties != null)
                    ?.RunProperties?.CloneNode(true) as RunProperties;
                if (srcRp != null)
                {
                    var para = cell.Elements<Paragraph>().FirstOrDefault();
                    if (para == null) { para = new Paragraph(); cell.Append(para); }
                    para.Append(new Run(srcRp));
                }
            }
            SetCellText(cell, text);
        }

        /// <summary>
        /// 填页脚温湿度格子: 数值**居中**于整条横线并加下划线(写在横线上、数字带下划线让横线连贯),
        /// 两侧仍用 _ 补足到模板原长(补齐 _ 不带下划线防双线), 后缀(°C/%RH)无下划线跟在行尾。
        /// 保留单元格原样式, 空值场景不调用此方法。
        ///
        /// 为什么拆多个 run: 一个 run 只能有一个下划线属性, 而我们要"数值有下划线、补齐 _/单位无"。
        /// 每个 run 必须各自 CloneNode 样式 —— 若共用同一个 rp 对象插到多处, OpenXml 会报 "part of a tree"。
        /// </summary>
        private void SetFooterValue(TableCell? cell, string value, string suffix)
        {
            if (cell == null) return;

            var refRun = cell.Descendants<Run>().FirstOrDefault(r => r.RunProperties != null);
            var rp = refRun?.RunProperties?.CloneNode(true) as RunProperties;

            // 原格横线长(_ 个数)必须先取: 下面整格清空后就没参照了; 模板横线长 = 填后横线目标总长
            int blankCols = cell.InnerText.Count(ch => ch == '_');

            // 值居中: 两侧 _ 大致对半分(左 floor 右 ceil), 数字段自带下划线让横线连贯
            int pad = Math.Max(0, blankCols - value.Length);
            int lead = pad / 2, trail = pad - lead;

            var paragraphs = cell.Elements<Paragraph>().ToList();
            for (int i = 1; i < paragraphs.Count; i++) paragraphs[i].Remove();
            var para = paragraphs.FirstOrDefault();
            if (para == null) { para = new Paragraph(); cell.Append(para); }

            foreach (var run in para.Elements<Run>().ToList()) run.Remove();

            // 左侧补齐 _ (无下划线: 模板横线本就是 _ 字形, 再下划线会成双线)
            if (lead > 0)
                para.Append(BuildPadRun(rp, lead));

            // 数值 run: 复制原样式 + 加下划线 (居中位置, 数字带下划线让横线不断)
            var valRun = new Run((rp ?? new RunProperties()).CloneNode(true) as RunProperties ?? new RunProperties());
            valRun.RunProperties!.Underline = new Underline { Val = UnderlineValues.Single };
            valRun.Append(new Text(value));
            para.Append(valRun);

            // 右侧补齐 _
            if (trail > 0)
                para.Append(BuildPadRun(rp, trail));

            // 后缀 run: 原样式, 无下划线
            var sfxRun = new Run((rp ?? new RunProperties()).CloneNode(true) as RunProperties ?? new RunProperties());
            sfxRun.Append(new Text(suffix));
            para.Append(sfxRun);
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
