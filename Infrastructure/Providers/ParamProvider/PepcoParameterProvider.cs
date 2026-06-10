using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;


namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider
{
    public class PepcoParameterProvider
    {
        private readonly FiberContentHelper _helper;

        public PepcoParameterProvider(FiberContentHelper helper)
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
                WashingProcedure = p.WashingProcedure,
                DryProcedure = DryProcedureHelper(p.SampleDescription!, p.DryProcedure),
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
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure,
                DryProcedure = DryProcedureHelper(p.SampleDescription!, p.DryProcedure),
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Air Permeability", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure,
                DryProcedure = DryProcedureHelper(p.SampleDescription!, p.DryProcedure),
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
    : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
    : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Water Repellency-Spray Test", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure,
                DryProcedure = DryProcedureHelper(p.SampleDescription!, p.DryProcedure),
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
: _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
: "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Water Resistance-Hydrostatic Pressure", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure,
                DryProcedure = DryProcedureHelper(p.SampleDescription!, p.DryProcedure),
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
: _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
: "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Print Durability", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure,
                DryProcedure = DryProcedureHelper(p.SampleDescription!, p.DryProcedure),
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Detergent = "160g ECE Detergent+40g Sodium Perborate",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
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
            if (ItemName == "Pilling Resistance")
            {
                Condition = infoDto.sampleDescription!.Contains("Knit") ? "Knit" : "Woven";
            }
            if (ItemName == "Air Permeability")
            {
                if (infoDto.sampleDescription!.Contains("WindProof")) Condition = "As Received";
                else if (infoDto.sampleDescription.Contains("Breathability")) Condition = "After 3 Wash";
                else Condition = null;
            }
            if (ItemName == "CF to Perspiration")
            {
                if (infoDto.sampleDescription!.Contains("HomeTextile")) Condition = "Common";
                else Condition = "LyoW";
            }
            if (ItemName == "CF to Water")
            {
                if (infoDto.sampleDescription!.Contains("HomeTextile")) Condition = "Common";
                else Condition = "LyoW";
            }
            if (ItemName == "CF to Washing")
            {
                if (infoDto.sampleDescription!.Contains("HomeTextile")) Condition = "Common";
                else Condition = "LyoW";
            }
            return GetParameter(ItemName, Condition);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Item, string? Lv), string?> _map = new()
        {
            [("Pilling Resistance", "Woven")] = "Cycle: 2000 revs",
            [("Pilling Resistance", "Knit")] = "Cycle: 500 revs",
            [("Water Resistance-Hydrostatic Pressure", null)] = "1600mmH2O，Original & 1 Wash",
            [("Air Permeability", "As Received")] = "As Received; Area:20cm²",
            [("Air Permeability", "After 3 Wash")] = "After 3 Wash; Area:20cm²",
            [("Air Permeability", null)] = "Please Select 'WindProof' or 'Breathability'",
            [("Wicking", null)] = "As Received",
            [("Absorbency", null)] = "As Received",
            [("Water Repellency-Spray Test", null)] = "Water-Resistant Test as Recevied; Water-Repllent Test After 3 Wash.",
            [("Seam Slippage", null)] = "The negative load depends on the basis weight",
            [("Drying Rate of Fabrics", null)] = "As Received",
            [("CF to Perspiration", "Common")] = "Multi-Fibre:DW",
            [("CF to Perspiration", "LyoW")] = "Multi-Fibre: LyoW",
            [("CF to Water", "Common")] = "Multi-Fibre:DW",
            [("CF to Water", "LyoW")] = "Multi-Fibre: LyoW",
            [("CF to Washing", "Common")] = "Multi-Fibre:DW",
            [("CF to Washing", "LyoW")] = "Multi-Fibre: LyoW",
        };

        private static string? GetParameter(string item, string? lv)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((item, lv), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((item, null), out var fallback)) return fallback;

            return null!;
        }



        private string? DryProcedureHelper(string sampleDesc, string? dryProcedure)
        {
            if (string.IsNullOrEmpty(dryProcedure) == false) return dryProcedure;
            else
            {
                if (sampleDesc.Contains("Woven")) return "Line Dry";
                else if (sampleDesc.Contains("Knit")) return "Flat Dry";
                else return "Line Dry";
            }

        }
    }
}
