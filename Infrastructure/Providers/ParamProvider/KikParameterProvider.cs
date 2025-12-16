using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using DocumentFormat.OpenXml.Presentation;


namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider
{
    public class KikParameterProvider
    {
        private readonly FiberContentHelper _helper;

        public KikParameterProvider(FiberContentHelper helper)
        {
            _helper = helper;
        }
        //仅仅用于修改对应ItemName中的Parameter
        public WetParameterIso CreateWetParameters(ParamsInput p) => (p.ItemName, p.WashingProcedure, p.DCProcedure) switch
        {
            ("CF to Washing", "4N" or "4M" or "4G" or "3N", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Temperature = p.WashingProcedure.Contains("3") == true ? "30" : "40",
                Program = p.WashingProcedure.Contains("3") == true ? "ref A2S" : "A2S",
                SteelBallNum = _helper.IsCompositionExist("Animal", p.FiberContent!) == true ? 0 : 10
            },
            ("CF to Washing", "4H" or "3M" or "3G" or "3H", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Temperature = p.WashingProcedure.Contains("3") == true ? "30" : "40",
                Program = p.WashingProcedure.Contains("3") == true ? "ref A2S" : "A2S",
                SteelBallNum = 0
            },
            ("DS to Washing", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure!.Contains("N") ? "Cotton procedure"
                : p.WashingProcedure!.Contains("M") ? "Minimum iron procedure"
                : p.WashingProcedure!.Contains("G") ? "Delicates procedure"
                : "Wollens procedure",
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Program = "900r"
            },
            ("Appearance", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure!.Contains("N") ? "Cotton procedure"
                : p.WashingProcedure!.Contains("M") ? "Minimum iron procedure"
                : p.WashingProcedure!.Contains("G") ? "Delicates procedure"
                : "Wollens procedure",
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Program = "900r"
            },
            ("Attachment Strength", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure,
                DryProcedure = "Tumble Dry",
                Temperature = p.WashingProcedure!.Contains("4") ? "50" : "40",
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
            ("Spirality/Skewing", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure!.Contains("N") ? "Cotton procedure"
                : p.WashingProcedure!.Contains("M") ? "Minimum iron procedure"
                : p.WashingProcedure!.Contains("G") ? "Delicates procedure"
                : "Wollens procedure",
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Program = "900r"
            },
            _ => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!
            }
        };


        public async Task<string?> CreateParameters([FromBody] RequiredInfoDto infoDto, string ItemName)
        {

            // 1. 计算最大值
            string? Condition = null;
            if (ItemName == "CF to Light")
            {
                if (infoDto.sampleDescription!.Contains("General")) Condition = "L-4";
                else if (infoDto.sampleDescription.Contains("Swimwear") || infoDto.sampleDescription.Contains("Ski Wear")) Condition = "L-5";
                else if (infoDto.sampleDescription.Contains("Swimwear") && (infoDto.sampleDescription.Contains("Neon") || infoDto.sampleDescription.Contains("Turquoise"))) Condition = "L-3";
            }
            if (ItemName == "Water Resistance-Hydrostatic Pressure")
            {
                if (infoDto.sampleDescription!.Contains("General")) Condition = "1500";
                else if (infoDto.sampleDescription.Contains("Swimwear") || infoDto.sampleDescription.Contains("Ski Wear")) Condition = "3000";
                else if (infoDto.sampleDescription.Contains("Sealed Seams")) Condition = "800";
                else if (infoDto.sampleDescription.Contains("Non-sealed Seams")) Condition = "0";
            }
            if (ItemName == "Pilling Resistance")
            {
                if (infoDto.sampleDescription!.Contains("Anti-pilling")) Condition = "2000";
                else if (infoDto.sampleDescription.Contains("Woven") || infoDto.sampleDescription.Contains("Knit") && infoDto.sampleDescription.Contains("Tights")) Condition = "1000";
                else if (infoDto.sampleDescription.Contains("knit")) Condition = "500";
            }
            return GetParameter(ItemName, Condition);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Item, string? Lv), string?> _map = new()
        {
            [("Appearance", null)] = "Same as Dimensional Stability to Washing",
            [("Air Permeability", null)] = "Test Area: 20cm²",
            [("CF to Light", null)] = "General: L-4\r\nSwim wear, Ski wear: L-5\r\nSwim wear (color neon and turquoise) : L-3",
            [("CF to Light", "L-3")] = "L-3",
            [("CF to Light", "L-4")] = "L-4",
            [("CF to Light", "L-5")] = "L-5",
            [("CF to Light", null)] = "L-5",
            [("Water Resistance-Hydrostatic Pressure", null)] = "function wear(general):1500mmH2O\r\nfunction wear(ski):3000mmH2O\r\nsealed seams: 800mmH2O\r\nnon-sealed seams: 0",
            [("Water Resistance-Hydrostatic Pressure", "1500")] = "function wear(general):1500mmH2O",
            [("Water Resistance-Hydrostatic Pressure", "3000")] = "3000mmH2O",
            [("Water Resistance-Hydrostatic Pressure", "800")] = "800mmH2O",
            [("Water Resistance-Hydrostatic Pressure", "0")] = "0",
            [("CF to Chlorinated Water", null)] = "50mg/L",
            [("Pilling Resistance", null)] = "Articles with anti-pilling finishing:\r\n2000 cycles: Grade 3-4\r\nWoven fabric, Knitted fabric (incl. tights, leggings):\r\n1000 cycles: Grade 3-4\r\nCoarse knit (</= 12 gauge):\r\n 500 cycles: Grade 2-3\r\n",
            [("Pilling Resistance", "500")] = "500 revs",
            [("Pilling Resistance", "1000")] = "1000 revs",
            [("Pilling Resistance", "2000")] = "2000 revs",
        };

        private static string? GetParameter(string item, string? lv)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((item, lv), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((item, null), out var fallback)) return fallback;

            return null!;
        }

    }
}
