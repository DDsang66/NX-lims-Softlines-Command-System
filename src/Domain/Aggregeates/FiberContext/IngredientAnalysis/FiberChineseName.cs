using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis
{
    /// <summary>
    /// 国标报告的**中文化** —— 闸门、纤维名的拼法、五个标签的替换表，三件事都在这一处。
    ///
    /// **闸门只认一个值**：<c>FZ/T 01057</c>。method 下拉里没有 "GB/T …" 这个选项
    /// （见 <c>FiberWorksheetService.MethodOptions</c>），国产项就这一个系列名，
    /// 它由 <see cref="FiberStandardChainBuilder"/> 展开出 <c>.1~.4</c> 与 <c>GB/T 2910.x</c>。
    /// 刻意**不**按"链里含 GB/T"判 —— 链里同时混着 FZ/T 与 ISO/TR 11827，字符串判会误伤。
    /// 也**不**认 <c>FZ/T 01057.1-4–2007</c> 这类库里存的旧串：那是老记录，不是勾选值。
    ///
    /// 与 AATCC 那支天然互斥（<c>IngredientAnalysisCalculation.GenerateRecommendedLabel</c>
    /// 里的 isAatcc 同样读本段标准），两支不会同时成立。
    /// </summary>
    internal static class FiberChineseName
    {
        /// <summary>触发中文化的那个 method 值。</summary>
        public const string GbStandard = "FZ/T 01057";

        /// <summary>
        /// 报告模板里那五个**纯文本**标签 → 中文。
        ///
        /// 它们在 .docx 里是光秃秃的文字、连书签都没有，所以换不了"值"，
        /// 只能让引擎按整段文字匹配后改写（见 <c>WordTemplateEngine.ReplaceText</c> 的
        /// <c>replaceParagraphText</c> 参数）。
        ///
        /// 冒号用**半角** —— 与模板里英文那行同宽，且与工作簿里 `单号：`/`测试方法：` 的
        /// 全角风格不一致是刻意的（这批标签对齐的是英文原文的版式）。
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> ReportLabels = new Dictionary<string, string>
        {
            ["Test Result:"] = "测试结果:",
            ["Based on moisture regain weight:"] = "结合公定回潮率的含量:",
            ["Recommendation:"] = "推荐标签:",
            ["Conclusion:"] = "结论:",
            ["Remark:"] = "备注:",
        };

        /// <summary>本段这一份是否出中文。</summary>
        public static bool IsChineseReport(string? standard)
            => standard?.Trim().Equals(GbStandard, StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>
        /// 纤维名 → <c>中文名 + 英文原名</c>（如 <c>棉Cotton</c>）；查不到就原样返回英文。
        ///
        /// 中文在前、英文紧跟、**中间不加空格** —— 与原工作簿 `绵羊毛wool` 那 25 条的写法一致。
        /// 查表时 Trim、返回时保留原样：名字是别的通道来的，不该在这里被悄悄规整。
        /// </summary>
        public static string Localize(string? name, IReadOnlyDictionary<string, string>? map)
        {
            if (string.IsNullOrWhiteSpace(name)) return name ?? string.Empty;
            if (map == null || map.Count == 0) return name;

            return map.TryGetValue(name.Trim(), out var cn) && !string.IsNullOrWhiteSpace(cn)
                ? cn + name
                : name;
        }
    }
}
