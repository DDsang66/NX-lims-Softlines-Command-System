using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using DocumentFormat.OpenXml.Math;


namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider
{
    public class LPPParameterProvider
    {
        private readonly FiberContentHelper _helper;

        public LPPParameterProvider(FiberContentHelper helper)
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
                AfterWash = "After 3 Washes",
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
                ReportNumber = p.OrderNumber!,
                Standard = p.Standard,
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
    : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
    : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = "After 3 Washes",
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Water Resistance-Hydrostatic Pressure", _, _) => new WetParameterIso
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
                AfterWash = "After 3 Washes",
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
                case "Pilling Resistance":
                    if(standard.Contains("12945-1")) condition = "ICI";
                    else if (standard.Contains("12945-2")) condition = "Martindale";

                    if(_helper.CompositionRate(infoDto.fiberComposition!,"Wool")>0) condition1 = "Wool";
                    else if (infoDto.sampleDescription!.Contains("Woven")) condition1 = "Woven";
                    else if (infoDto.sampleDescription.Contains("Knit")) condition1 = "Knit";
                    break;
                case "Tear Strength":
                    if (infoDto.sampleDescription!.Contains("Textile")) condition = "textile";
                    else if (infoDto.sampleDescription.Contains("Denim")) condition = "denim";
                    else if (infoDto.sampleDescription.Contains("Leather")) condition = "leather";
                    break;
                case "Tensile Strength":
                    if (infoDto.sampleDescription!.Contains("Fabric")) condition = "fabric";    
                    else if (infoDto.sampleDescription.Contains("Leather")) condition = "leather";

                    if (infoDto.sampleDescription.Contains("Woven")) condition1 = "Woven";
                    else if (infoDto.sampleDescription.Contains("Knit")) condition1 = "knit";
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
                case "Zipper Strength":
                    if (infoDto.sampleDescription!.Contains("Fastener durability test")) condition = "Fastener durability test";
                    else if (infoDto.sampleDescription.Contains("Puller strength")) condition = "Puller strength";
                    else if (infoDto.sampleDescription.Contains("Zipper resistance")) condition = "Zipper resistance";
                    break;

            }
            return GetParameter(ItemName, condition, condition1);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Item, string? Condition, string? Condition1), string?> _map = new()
        {
            [("Seam Slippage", null, null)] = "{ ≤ 220 g/m² Load applied: 60N; > 220 g/m² Load applied: 120N}",
            [("Pilling Resistance", "ICI", "Wool")] = "Cycle: 7200 revs",
            [("Pilling Resistance", "ICI", "Knit")] = "Cycle: 14400 revs",
            [("Pilling Resistance", "Martindale", "Woven")] = "Cycle: 2000 revs",
            [("Pilling Resistance", "Martindale", "Knit")] = "Cycle: 2000 revs",
            [("Tear Strength", "textile", null)] = "{ ≤90 gsm Load > 5N；90-149 gsm Load > 10N；150-200 gsm Load > 15N；>200 gsm Load >16N}",
            [("Tear Strength", "denim", null)] = "{> 270 gsm untreated：16N，chemical treated：15N；> 370 gsm untreated：20N，chemical treated：16N}",
            [("Tear Strength", "leather", null)] = "Load > 30N",
            [("Tensile Strength", "leather", null)] = "Load > 10N/mm",
            [("Tensile Strength", "fabric", "Woven")] = "{≤150 gsm Load：140N；150-250 gsm Load：250N；>250 gsm Load：250N}",
            [("Tensile Strength", "fabric", "knit")] = "N/A",
            [("Abrasion Resistance", null, null)] = "Cycle 20000 revs; Color Change @ 5000 revs",
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
            [("CF to Light", null, null)] = "L-4",
            [("CF to Washing", null, null)] = "After 5 Washes",
            [("Attachment Strength", null, null)] = "90N 10s",
            [("Seam Strength", null, null)] = "Load：70N",
            [("Water Resistance-Hydrostatic Pressure", null, null)] = "2000mm H2O，After 5 Washes",
            [("Water Repellency-Spray Test", null, null)] = "Before and after 5 Washes",
            [("Air Permeability", null, null)] = "< 20 mm/s ",
            [("Quick Dry", null, null)] = "After 30 min ≤ 0.04 ml",
            [("Zipper Strength", "Fastener durability test", null)] = "Buttons, snap fasteners, etc.: 70N 10s",
            [("Zipper Strength", "Puller strength", null)] = "≥200N",
            [("Zipper Strength", "Zipper resistance", null)] = "≥500 cycles without failure",
        };

        private static string? GetParameter(string Item, string? Condition, string? Condition1)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((Item, Condition, Condition1), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((Item, Condition, null), out var fallback)) return fallback;

            return null!;
        }

    }
}
