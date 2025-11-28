using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider
{
    public class NextParameterProvider
    {
        private readonly FiberContentHelper _helper;

        public NextParameterProvider(FiberContentHelper helper)
        {
            _helper = helper;
        }

        public WetParameterIso CreateWetParameters(ParamsInput p) => (p.ItemName, p.WashingProcedure, p.DCProcedure) switch
        {
            ("CF to Washing", "4N" or "4M" or "4G" or "3N", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
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
            _ => new WetParameterIso
            {
                ContactItem = p.ItemName,
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
            return GetParameter(ItemName, Condition);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Item, string? Lv), string?> _map = new()
        {
            [("Fastness to Light", null)] = "L-4",
            [("Fastness to Washing", null)] = "Multi-Fibre Type:LW",
            [("Cross Staining to Washing", null)] = "Multi-Fibre Type:LW",
            [("Fastness to Dry Cleaning", null)] = "Multi-Fibre Type:LW",
            [("Cross Staining to Dry Cleaning", null)] = "Multi-Fibre Type:LW",
            [("Fastness to Water", null)] = "Multi-Fibre Type:LW",
            [("Cross Staining to Water", null)] = "Multi-Fibre Type:LW",
            [("Fastness to Chlorinated Water", null)] = "50mg/L",
            [("Stability to Dry Cleaning", null)] = "Wash Procedure: Commercial dry-cleaning",
            [("Appearance Assessment after Dry Clean", null)] = "Wash Procedure: Commercial dry-cleaning",
            [("Grab Strength & Seam Slippage", null)] = "Need additional unit weight",
            [("Seam Slippage of Garment Seams", null)] = "Need additional unit weight",
            [("Tear Strength", null)] = "Need additional unit weight",
            [("Martindale Abrasion", null)] = "9KPa,Shade Change @ 5000 {≤150g/m²: 10000 rubs；150-250g/m²: 15000 rubs；≥250g/m²: 20000 rubs}",
            [("Abrasion Home", null)] = "12KPa；Cycle：20000revs；Shade Change @ 6000&10000",
            [("Bursting Strength", null)] = "Diameter: 30.5mm",
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
