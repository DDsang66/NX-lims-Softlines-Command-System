using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using DocumentFormat.OpenXml.Math;


namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider
{
    public class LTAGParameterProvider
    {
        private readonly FiberContentHelper _helper;

        public LTAGParameterProvider(FiberContentHelper helper)
        {
            _helper = helper;
        }
        //仅仅用于修改对应ItemName中的Parameter
        public WetParameterAatcc CreateWetParameters(ParamsInput p) => (p.ItemName, p.WashingProcedure, p.DCProcedure) switch
        {
            ("CF to Washing", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure,
                Temperature ="120",
                Program = "2A",
                Detergent = "0.15",
                SteelBallNum = 50,
                SteelBallType = "Steel"
            },
            ("DS to Washing", "Hand Wash Cold" or "Hand Wash", _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("Cold") ? "80" : "105",
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("DS to Washing", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("Cold") ? "80"
                : p.WashingProcedure.Contains("Warm") ? "105"
                : p.WashingProcedure.Contains("Hot") ? "120"
                : "140",
                Cycle = p.WashingProcedure!.Contains("Normal") ? "Normal"
                : p.WashingProcedure.Contains("Gentle") ? "Gentle"
                : p.WashingProcedure.Contains("Permanent Press") ? "Permanent"
                : "",
                DryCondition = DryConditionHelper(p.DryProcedure!),
                Detergent=p.Detergent!.Contains("Mild Detergent")?"Woolite Detergent":"60g Tide powder",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Appearance", "Hand Wash Cold" or "Hand Wash", _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                WashingProcedure = p.WashingProcedure,
                Temperature =p.WashingProcedure!.Contains("Cold") ? "80" : "105",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Appearance", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("Cold") ? "80"
                : p.WashingProcedure.Contains("Warm") ? "105"
                : p.WashingProcedure.Contains("Hot") ? "120"
                : "140",
                Cycle = p.WashingProcedure!.Contains("Normal") ? "Normal"
                : p.WashingProcedure.Contains("Gentle") ? "Gentle"
                : p.WashingProcedure.Contains("Permanent Press") ? "Permanent"
                : "",
                DryCondition = DryConditionHelper(p.DryProcedure!),
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                DryCleanProcedure= p.DryProcedure??null,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
            },
            ("DS to Dry-clean", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,

            },
            ("Spirality/Skewing", "Hand Wash Cold" or "Hand Wash", _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                WashingProcedure = p.WashingProcedure,
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                Temperature =
                p.WashingProcedure!.Contains("Cold") ? "80" : "105",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
                DryCleanProcedure = p.DryProcedure ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                SpecialCareInstruction = p.Sci ?? null,
            },
            ("Spirality/Skewing", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("Cold") ? "80"
                : p.WashingProcedure.Contains("Warm") ? "105"
                : p.WashingProcedure.Contains("Hot") ? "120"
                : "140",
                Cycle = p.WashingProcedure!.Contains("Normal") ? "Normal"
                : p.WashingProcedure.Contains("Gentle") ? "Gentle"
                : p.WashingProcedure.Contains("Permanent Press") ? "Permanent"
                : "",
                DryCondition = DryConditionHelper(p.DryProcedure!),
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
                DryCleanProcedure = p.DryProcedure??null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Pilling Resistance", "Hand Wash Cold" or "Hand Wash", _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("Cold") ? "80" : "105",
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Pilling Resistance", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("Cold") ? "80"
                : p.WashingProcedure.Contains("Warm") ? "105"
                : p.WashingProcedure.Contains("Hot") ? "120"
                : "140",
                Cycle = p.WashingProcedure!.Contains("Normal") ? "Normal"
                : p.WashingProcedure.Contains("Gentle") ? "Gentle"
                : p.WashingProcedure.Contains("Permanent Press") ? "Permanent"
                : "",
                DryCondition = DryConditionHelper(p.DryProcedure!),
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Water Repellency-Spray Test", "Hand Wash Cold" or "Hand Wash", _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("Cold") ? "80" : "105",
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Water Repellency-Spray Test", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("Cold") ? "80"
                : p.WashingProcedure.Contains("Warm") ? "105"
                : p.WashingProcedure.Contains("Hot") ? "120"
                : "140",
                Cycle = p.WashingProcedure!.Contains("Normal") ? "Normal"
                : p.WashingProcedure.Contains("Gentle") ? "Gentle"
                : p.WashingProcedure.Contains("Permanent Press") ? "Permanent"
                : "",
                DryCondition = DryConditionHelper(p.DryProcedure!),
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Drying Rate of Fabrics", "Hand Wash Cold" or "Hand Wash", _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("Cold") ? "80" : "105",
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Drying Rate of Fabrics", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("Cold") ? "80"
                : p.WashingProcedure.Contains("Warm") ? "105"
                : p.WashingProcedure.Contains("Hot") ? "120"
                : "140",
                Cycle = p.WashingProcedure!.Contains("Normal") ? "Normal"
                : p.WashingProcedure.Contains("Gentle") ? "Gentle"
                : p.WashingProcedure.Contains("Permanent Press") ? "Permanent"
                : "",
                DryCondition = DryConditionHelper(p.DryProcedure!),
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Absorbency", "Hand Wash Cold" or "Hand Wash", _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("Cold") ? "80" : "105",
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Absorbency", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("Cold") ? "80"
                : p.WashingProcedure.Contains("Warm") ? "105"
                : p.WashingProcedure.Contains("Hot") ? "120"
                : "140",
                Cycle = p.WashingProcedure!.Contains("Normal") ? "Normal"
                : p.WashingProcedure.Contains("Gentle") ? "Gentle"
                : p.WashingProcedure.Contains("Permanent Press") ? "Permanent"
                : "",
                DryCondition = DryConditionHelper(p.DryProcedure!),
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Air Permeability", "Hand Wash Cold" or "Hand Wash", _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("Cold") ? "80" : "105",
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Air Permeability", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("Cold") ? "80"
                : p.WashingProcedure.Contains("Warm") ? "105"
                : p.WashingProcedure.Contains("Hot") ? "120"
                : "140",
                Cycle = p.WashingProcedure!.Contains("Normal") ? "Normal"
                : p.WashingProcedure.Contains("Gentle") ? "Gentle"
                : p.WashingProcedure.Contains("Permanent Press") ? "Permanent"
                : "",
                DryCondition = DryConditionHelper(p.DryProcedure!),
                Detergent = p.Detergent!.Contains("Mild Detergent") ? "Woolite Detergent" : "60g Tide powder",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            _ => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!
            }
        };


        public async Task<string?> CreateParameters([FromBody] RequiredInfoDto infoDto, string ItemName, string standard)
        {

            // 1. 计算最大值
            string? condition = null;
            string? condition1 = null;
            switch (ItemName) 
            {
                case "Abrasion Resistance":
                    if (infoDto.sampleDescription!.Contains("Outerwear")) condition = "25000";
                    else if (infoDto.sampleDescription!.Contains("Bottom")|| infoDto.sampleDescription!.Contains("Denim")) condition = "35000";
                    if (infoDto.sampleDescription!.Contains("Shirts")) condition = "15000"; 
                    break;
                case "Extension and Recovery":
                    if (infoDto.sampleDescription!.Contains("Woven")&&standard.Contains("3107")) condition = "Woven";
                    else if (infoDto.sampleDescription!.Contains("Knit") && standard.Contains("2594")) condition = "Knit";
                    break;
                case "Pilling Resistance":
                    if (infoDto.sampleDescription!.Contains("Anti-Pilling")) condition = "Anti";
                    break;

            }
            return GetParameter(ItemName, condition, condition1);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Item, string? Condition, string? Condition1), string?> _map = new()
        {
            [("CF to Light", null, null)] = "20AFU",
            [("Dye Transfer in Storage", null, null)] = "Option 1；Temperature：75°F；Time：48 houres",
            [("Abrasion Resistance", "25000", null)] = "Cycle：25000 rubs",
            [("Abrasion Resistance", "35000", null)] = "Cycle：35000 rubs",
            [("Abrasion Resistance", "15000", null)] = "Cycle：15000 rubs",
            [("Extension and Recovery", "Woven", null)] = "Growth：5% after 30 mins； Recovery：85% minimum",
            [("Extension and Recovery", "Knit", null)] = "Growth：7% after 30 min； Recovery：85% minimum",
            [("Pilling Resistance", "Anti", null)] = "@30min；Original Sample & After 3 Washes",
            [("Pilling Resistance", null, null)] = "@30min；",
            [("Water Repellency-Spray Test", null, null)] = "Original and 20 Washes；",
            [("Drying Rate of Fabrics", null, null)] = "Original, 5 & 10 Washes；30min；",
            [("Absorbency", null, null)] = "Original, 5 & 10 Washes；",
            [("Water Resistance-Rain Test", null, null)] = "Pressure：600 mmH2O；",
            [("Air Permeability",null,null)] = "Original and 5 Washes",
        };

        private static string? GetParameter(string Item, string? Condition, string? Condition1)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((Item, Condition, Condition1), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((Item, Condition, null), out var fallback)) return fallback;

            return null!;
        }



        private string? WetParamHelper(string WashingProcedure)
        {
            if (WashingProcedure == null) return null;
            string part_1 = "";
            string part_2 = "";
            part_1 =
            WashingProcedure!.Contains("Normal") ? "(1)"
            : WashingProcedure.Contains("Gentle") ? "(2)"
            : WashingProcedure.Contains("Permanent") ? "(3)"
            : "";
            part_2 =
                WashingProcedure!.Contains("Cold") ? "II"
                : WashingProcedure.Contains("Warm") ? "III"
                : WashingProcedure.Contains("Hot") ? "IV"
                : "V";
            string program = part_1 + part_2;
            return program;
        }


        private string? DryConditionHelper(string DryProcedure)
        {
            if (DryProcedure == null) return null;
            string program = "";
            program =
                DryProcedure!.Contains("Low") ? "A(ii)"
                : DryProcedure.Contains("Line Dry") ? "B"
                : DryProcedure.Contains("Flat Dry") ? "D"
                : "A(i)";
            return program;
        }

    }
}
