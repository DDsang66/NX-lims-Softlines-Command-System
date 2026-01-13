using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using DocumentFormat.OpenXml.Math;


namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider
{
    public class FocusParameterProvider
    {
        private readonly FiberContentHelper _helper;

        public FocusParameterProvider(FiberContentHelper helper)
        {
            _helper = helper;
        }
        //仅仅用于修改对应ItemName中的Parameter
        public WetParameterIso CreateWetParameters(ParamsInput p) => (p.ItemName, p.WashingProcedure, p.DCProcedure) switch
        {
            ("CF to Washing", "4N" or "4M" or "4G" or "3N", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Standard = p.Standard,
                Temperature = p.WashingProcedure.Contains("3") == true ? "30" : "40",
                Program = p.WashingProcedure.Contains("3") == true ? "ref A2S" : "A2S",
                SteelBallNum = _helper.IsCompositionExist("Animal", p.FiberContent!) == true ? 0 : 10
            },
            ("CF to Washing", "4H" or "3M" or "3G" or "3H", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Standard = p.Standard,
                Temperature = p.WashingProcedure.Contains("3") == true ? "30" : "40",
                Program = p.WashingProcedure.Contains("3") == true ? "ref A2S" : "A2S",
                SteelBallNum = 0
            },
            ("DS to Washing", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Standard = p.Standard,
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Appearance", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Standard = p.Standard,
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
    : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
    : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("DS to Dry-clean", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null
            },
            ("Water Repellency-Spray Test", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("3") ? "80"
                : p.WashingProcedure.Contains("4") ? "105"
                : p.WashingProcedure.Contains("5") ? "120"
                : "140",
                Bleach = p.WashingProcedure!.Contains("N") ? "Normal"
                : p.WashingProcedure.Contains("G") ? "Gentle"
                : p.WashingProcedure.Contains("M") ? "Permanent Press"
                : "",
                //Cycle，程度暂时用Bleach字段代替
                DryCleanProcedure = DryConditionHelper(p.DryProcedure!),
                //DryCondition，暂时用干洗字段代替
                AfterWash = "1 Wash",
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
            },
            ("Absorbency" ,_, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("3") ? "80"
                : p.WashingProcedure.Contains("4") ? "105"
                : p.WashingProcedure.Contains("5") ? "120"
                : "140",
                Bleach = p.WashingProcedure!.Contains("N") ? "Normal"
                : p.WashingProcedure.Contains("G") ? "Gentle"
                : p.WashingProcedure.Contains("M") ? "Permanent Press"
                : "",
                //Cycle，程度暂时用Bleach字段代替
                DryCleanProcedure = DryConditionHelper(p.DryProcedure!),
                //DryCondition，暂时用干洗字段代替
                AfterWash = "1 Wash",
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
            },
            ("Tear Strength", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("3") ? "80"
                : p.WashingProcedure.Contains("4") ? "105"
                : p.WashingProcedure.Contains("5") ? "120"
                : "140",
                Bleach = p.WashingProcedure!.Contains("N") ? "Normal"
                : p.WashingProcedure.Contains("G") ? "Gentle"
                : p.WashingProcedure.Contains("M") ? "Permanent Press"
                : "",
                //Cycle，程度暂时用Bleach字段代替
                DryCleanProcedure = DryConditionHelper(p.DryProcedure!),
                //DryCondition，暂时用干洗字段代替
                AfterWash = "1 Wash",
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
            },
            ("Tensile Strength", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("3") ? "80"
                : p.WashingProcedure.Contains("4") ? "105"
                : p.WashingProcedure.Contains("5") ? "120"
                : "140",
                Bleach = p.WashingProcedure!.Contains("N") ? "Normal"
                : p.WashingProcedure.Contains("G") ? "Gentle"
                : p.WashingProcedure.Contains("M") ? "Permanent Press"
                : "",
                //Cycle，程度暂时用Bleach字段代替
                DryCleanProcedure = DryConditionHelper(p.DryProcedure!),
                //DryCondition，暂时用干洗字段代替
                AfterWash = "1 Wash",
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
            },
            ("Seam Slippage", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Standard = p.Standard,
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
: _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
: "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = "After 1 Wash",
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Extension and Recovery", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Standard = p.Standard,
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
: _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
: "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = "After 1 Wash",
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            _ => new WetParameterIso
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
                case "CF to Light":
                    if(infoDto.sampleDescription!.Contains("30 hours")) condition = "30";
                    else condition = "60";
                    break;
                case "Abrasion Resistance":
                    if (infoDto.sampleDescription!.Contains("Foil Print")) condition = "9";
                    else condition = "12";
                    if (infoDto.sampleDescription.Contains("Fabric")) condition1 = "Fabric";
                    else condition1 = "Garment";
                    break;
                case "Extension and Recovery":
                    var content = _helper.CompositionRate(infoDto.fiberComposition!, "Elastane");
                    if (content == 0) condition = "N/A";
                    if (infoDto.sampleDescription!.Contains("Woven")) { condition = "Woven"; }
                    else if (infoDto.sampleDescription!.Contains("Knit"))
                    {
                        condition = "Knit";
                        if (content <= 5)
                        { condition1 = infoDto.sampleDescription.Contains("Stripe") ? "3" : infoDto.sampleDescription.Contains("Loop") ? "6" : null; }
                        else if (content < 12 && content > 5)
                        { condition1 = infoDto.sampleDescription.Contains("Stripe") ? "4" : infoDto.sampleDescription.Contains("Loop") ? "8" : null; }
                        else if (content >= 12 && content <= 20)
                        { condition1 = infoDto.sampleDescription.Contains("Stripe") ? "5" : infoDto.sampleDescription.Contains("Loop") ? "10" : null; }
                        else if (content > 20)
                        { condition1 = infoDto.sampleDescription.Contains("Stripe") ? "7" : infoDto.sampleDescription.Contains("Loop") ? "14" : null; }
                    }

                    break;
                case "Seam Slippage":
                    if (standard.Contains("13936-2")) condition = "1 Wash";
                    break;
                case "Tear Strength":
                   if (infoDto.sampleDescription!.Contains("After Wash")) condition = "1 Wash";
                    else if (infoDto.sampleDescription!.Contains("After Dry-clean")) condition = "Dry-clean";
                    break;
                case "Tensile Strength":
                    if (infoDto.sampleDescription!.Contains("After Wash")) condition = "1 Wash";
                    else if (infoDto.sampleDescription!.Contains("After Dry-clean")) condition = "Dry-clean";
                    break;
                case "Water Repellency-Spray Test":
                    if (infoDto.sampleDescription!.Contains("After Wash")) condition = "1 Wash";
                    else if (infoDto.sampleDescription!.Contains("After Dry-clean")) condition = "Dry-clean";
                    break;
            }
            return GetParameter(ItemName, condition, condition1);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Item, string? Condition, string? Condition1), string?> _map = new()
        {
            [("Abrasion Resistance", "12", null)] = "Load：12KPa；Cycle: 10000 rubs",
            [("Abrasion Resistance", "9", "Fabric")] = "Load：9KPa；Cycle: 1000 / 2000 / 5000 revs",
            [("Abrasion Resistance", "9", "Garment")] = "Load：9KPa；Cycle: 2000 / 5000 / 10000 revs",
            [("CF to Light", "20", null)] = "Method 5, Use water cooled Xenon arc lamp; After 20 hours.",
            [("CF to Light", "30", null)] = "Method 5, Use water cooled Xenon arc lamp; After 30 hours.",
            [("CF to Light", "60", null)] = "Method 5, Use water cooled Xenon arc lamp; After 60 hours.",
            [("Seam Slippage", "1 Wash" , null)] = "After 1 Wash",
            [("Pilling Resistance", null, null)] = "After 1 Wash, Cycle：2000 revs",
            [("CF to Chlorinated Water", null, null)] = "20mg/L",
            [("Water Repellency-Spray Test", "1 Wash", null)] = "After 1 Wash",
            [("Water Repellency-Spray Test", "Dry-clean", null)] = "After Dry-clean",
            [("Water Repellency-Spray Test", null, null)] = "As received sample",
            [("Tensile Strength", null, null)] = "Need unit weight",
            [("Tear Strength", null, null)] = "Need unit weight",
            [("Tensile Strength", "1 Wash", null)] = "Need unit weight; After 1 Wash",
            [("Tensile Strength", "After Dry-clean", null)] = "Need unit weight; After Dry-clean",
            [("Tear Strength", "1 Wash", null)] = "Need unit weight; After 1 Wash",
            [("Tear Strength", "After Dry-clean", null)] = "Need unit weight; After Dry-clean",
            [("Extension and Recovery", "N/A", null)] = "N/A",
            [("Extension and Recovery", "Woven", null)] = "Load: 30N",
            [("Extension and Recovery", "Knit", "3")] = "Load: 3N",
            [("Extension and Recovery", "Knit", "4")] = "Load: 4N",
            [("Extension and Recovery", "Knit", "5")] = "Load: 5N",
            [("Extension and Recovery", "Knit", "7")] = "Load: 7N",
            [("Extension and Recovery", "Knit", "6")] = "Load: 6N",
            [("Extension and Recovery", "Knit", "8")] = "Load: 8N",
            [("Extension and Recovery", "Knit", "10")] = "Load: 10N",
            [("Extension and Recovery", "Knit", "14")] = "Load: 14N",
            [("Colour Fastness to Chlorinated Water", null, null)] = "20 mg/L",
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
            WashingProcedure!.Contains("N") ? "(1)"
            : WashingProcedure.Contains("G") ? "(2)"
            : WashingProcedure.Contains("M") ? "(3)"
            :  WashingProcedure.Contains("H") ? "Hand Wash"
            : "";
            part_2 =
                WashingProcedure!.Contains("3") ? "II"
                : WashingProcedure.Contains("4") ? "III"
                : WashingProcedure.Contains("5") ? "IV"
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
