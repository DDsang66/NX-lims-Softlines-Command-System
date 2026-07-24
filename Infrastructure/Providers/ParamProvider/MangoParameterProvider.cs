using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider
{
    public class MangoParameterProvider
    {
        private readonly FiberContentHelper _helper;

        public MangoParameterProvider(FiberContentHelper helper)
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
                DryProcedure = DryProcedureHelper(p.MenuName!,p.DryProcedure),
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
            string? largestVarName = await _helper.MaxCompositionType(infoDto.fiberComposition!)!;
            string menuName = infoDto.menuName!;
            if (menuName == null) { return null; }

            var ElasticLoad = string.Empty;
            var rate = _helper.CompositionRate(infoDto.fiberComposition, "Elastane") + _helper.CompositionRate(infoDto.fiberComposition, "Spandex");
            if ((infoDto.menuName!).Contains("Woven")) return ElasticLoad = "30";
            if (rate.HasValue && rate < 5) return ElasticLoad = "15";
            if (rate.HasValue && 5 <= rate && rate < 11) return ElasticLoad = "20";
            if (rate.HasValue && rate >= 11) return ElasticLoad = "25";

            // 2. 根据 Menu/Item 组合查表
            return GetParameter(menuName, ItemName, largestVarName, ElasticLoad);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Menu, string Item, string? Lv,string? elasticLoad), string?> _map = new()
        {
            [("Knit(Mango)", "Pilling Resistance", "Vegetable",null)] = "Cycle: 14400 revs",
            [("Knit(Mango)", "Pilling Resistance", "Man-made", null)] = "Cycle: 10800 revs",
            [("Knit(Mango)", "Pilling Resistance", "Synthetic", null)] = "Cycle: 10800 revs",
            [("Knit(Mango)", "Pilling Resistance", "Animal", null)] = "Cycle: 7200 revs",
            [("Knit(Mango)", "Pilling Resistance", null, null)] = null,
            [("Knit(Mango)", "CF to Light", null, null)] = "Light: L-5",
            [("Knit(Mango)", "Extension and Recovery", null, "15")] = "Load: 15N",
            [("Knit(Mango)", "Extension and Recovery", null, "20")] = "Load: 20N",
            [("Knit(Mango)", "Extension and Recovery", null, "25")] = "Load: 25N",

            [("Woven(Mango)", "Water Resistance-Hydrostatic Pressure", null, null)] = "Pressure: 90cm H2O",
            [("Woven(Mango)", "Extension and Recovery", null, "30")] = "Load: 30N",
            [("Woven(Mango)", "CF to Light", null, null)] = "Light: L-5",
            [("Woven(Mango)", "Snagging Resistance", null, null)] = "Cycle: 600 revs",
            [("Woven(Mango)", "Pilling Resistance", null, null)] = "Cycle: 2000 revs",
            [("Woven(Mango)", "Abrasion Resistance", null, null)] = "Load: 9KPa,Cycle: 15000 revs",
        };

        private static string? GetParameter(string menu, string item, string? lv, string? elasticLoad)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((menu, item, lv, elasticLoad), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((menu, item, null,null), out var fallback)) return fallback;

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
