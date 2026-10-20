using System;
using System.Collections.Generic;
using System.Linq;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelPrintTool
{
    /// <summary>
    /// WET Excel 输出时 sheet 的固定业务顺序。
    /// 排序发生在文件生成之后，只能看到表名，所以这里维护「sheet 基名 → 档位」。
    /// 一张表覆盖多项测试时，归入最靠前的档（例如 CFtoWashing&Rubbing&Light 归 washing）。
    /// 未列出的表名（含所有 NEXT 的 TMxx）一律落「其他」档，档内保持原排序规则。
    /// </summary>
    public static class WetSheetOrder
    {
        public const int OtherRank = 14;

        public static readonly IReadOnlyDictionary<string, int> Ranks =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                // 1 DS to ironing
                ["DStoIroning"] = 1,

                // 2 DS to washing（&DC-* 为洗+干洗合并表，按洗涤排）
                ["DStoWashing"] = 2,
                ["DStoWashing-F"] = 2,
                ["DStoWashing-G"] = 2,
                ["DStoWashing-Acc"] = 2,
                ["DStoWashing-Jacket"] = 2,
                ["DStoWashing-Pants&Skirt"] = 2,
                ["DStoWashing-Boiler suits"] = 2,
                ["DStoWashing&DC-BBPSC"] = 2,
                ["DStoWashing&DC-BSS"] = 2,
                ["DStoWashing&DC-STPSD"] = 2,

                // 3 Spirality
                ["Spirality"] = 3,
                ["Spirality-F"] = 3,
                ["Spirality-G"] = 3,
                ["Spirality-Dryclean"] = 3,
                ["Spirality-Dryclean-F"] = 3,
                ["Spirality&Apperance"] = 3, // 模板里就是这个拼写

                // 4 Appearance
                ["AppearanceAfterWashing"] = 4,
                ["AppearanceAfterWashing-F"] = 4,
                ["AppearanceAfterWashing-G"] = 4,
                ["AppearanceAfterAging"] = 4,
                ["Appearance-PM01"] = 4,
                ["Appearance-PM01(Ass)"] = 4,
                ["Appearance-Common"] = 4,
                ["Smoothness Appearance"] = 4,

                // 5 水洗缩率
                ["PM01Washing-F"] = 5,
                ["PM01Washing-G"] = 5,
                ["PM01Washing-Gv1"] = 5,
                ["PM01Washing-Acc"] = 5,

                // 6 CF to washing
                ["CFtoWashing&Perspration&Water"] = 6,
                ["CFtoWashing&Rubbing&Light"] = 6,
                ["CFtoWashing-Only for Itochu"] = 6,
                ["CFtoWash&Rub&Lig&Wat"] = 6,
                ["CFtoWRL"] = 6,
                ["CFtoWRLS"] = 6,
                ["CFtoWRSL"] = 6,
                ["CFtoWSL&PVC"] = 6,

                // 7 CF to rubbing
                ["CFtoRubbing&Cl&Sea&yellow&light"] = 7,
                ["CFtoPWR"] = 7,

                // 8 CF to light
                ["CFtoDC&Cl-R&C-M&Pers-Light"] = 8,

                // 9 CF to water
                ["CFtoPWS"] = 9,
                ["CFtoPWD"] = 9,
                ["CFtoPWCl"] = 9,
                ["CFtoPerspiration&Water"] = 9,
                ["CFtoPerspiration&Water&Dryclean"] = 9,

                // 10 CF to perspiration
                ["CFtoPerspiration&Bleach"] = 10,

                // 11 海水
                ["CFtoSeaWater&Cl"] = 11,
                ["CFtoSeaWater&PVC"] = 11,
                ["Bleach&SeaWater"] = 11,
                ["CFtoCSYI"] = 11,

                // 12 氯水
                ["CFtoSHC"] = 12,
                ["CFtoSHCl"] = 12,

                // 13 热压
                ["Heat Pressing Test"] = 13,

                // 14 其他 = 未列出的表名，见 OtherRank
            };

        /// <summary>取 sheet 的业务档位；副本自动按基名归位。</summary>
        public static int Rank(string sheetName)
        {
            return Ranks.TryGetValue(BaseName(sheetName), out int rank) ? rank : OtherRank;
        }

        /// <summary>去掉 EPPlus 复制产生的 " (n)" / "_CopyN" 后缀。</summary>
        public static string BaseName(string sheetName)
        {
            string name = sheetName.Trim();

            if (name.EndsWith(")", StringComparison.Ordinal))
            {
                int open = name.LastIndexOf(" (", StringComparison.Ordinal);
                if (open > 0)
                {
                    string inner = name.Substring(open + 2, name.Length - open - 3);
                    if (inner.Length > 0 && inner.All(char.IsDigit))
                        return name.Substring(0, open);
                }
            }

            int copy = name.LastIndexOf("_Copy", StringComparison.Ordinal);
            if (copy > 0)
            {
                string suffix = name.Substring(copy + "_Copy".Length);
                if (suffix.Length > 0 && suffix.All(char.IsDigit))
                    return name.Substring(0, copy);
            }

            return name;
        }

        /// <summary>副本序号：基表为 0，副本为 1、2、…</summary>
        public static int CopyIndex(string sheetName)
        {
            string name = sheetName.Trim();

            if (name.EndsWith(")", StringComparison.Ordinal))
            {
                int open = name.LastIndexOf(" (", StringComparison.Ordinal);
                if (open > 0)
                {
                    string inner = name.Substring(open + 2, name.Length - open - 3);
                    if (inner.Length > 0 && inner.All(char.IsDigit)
                        && int.TryParse(inner, out int n))
                        return n;
                }
            }

            int copy = name.LastIndexOf("_Copy", StringComparison.Ordinal);
            if (copy > 0)
            {
                string suffix = name.Substring(copy + "_Copy".Length);
                if (suffix.Length > 0 && suffix.All(char.IsDigit)
                    && int.TryParse(suffix, out int n))
                    return n;
            }

            return 0;
        }
    }
}
