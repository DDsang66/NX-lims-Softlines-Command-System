using NX_lims_Softlines_Command_System.Models;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;


namespace NX_lims_Softlines_Command_System.Infrastructure.Providers
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
            ("CF to Washing", "4N" or "4M" or "4G" or "3N", _) =>new WetParameterIso{
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber,
                Temperature = p.WashingProcedure.Contains("3")==true ? "30":"40",
                Program = p.WashingProcedure.Contains("3") == true?"ref A2S": "A2S",
                SteelBallNum = _helper.IsCompositionExist("Animal", p.FiberContent!) == true ? 0 : 10
            },
            ("CF to Washing", "4H" or "3M" or "3G" or "3H", _) =>new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber,
                Temperature = p.WashingProcedure.Contains("3") == true ? "30" : "40",
                Program = p.WashingProcedure.Contains("3") == true ? "ref A2S" : "A2S",
                SteelBallNum = 0},
            ("DS to Washing", _, _) =>new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber,
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = (_helper.IsCompositionTypeExist("Cellulose", p.FiberContent!)
                + _helper.IsCompositionSourceExist("Vegetable", p.FiberContent!)
                + _helper.IsCompositionSourceExist("Man-made", p.FiberContent!)) >= 51 ? "Type I (100% cotton)" : "Type III (100% polyester)"},
            ("DS to Dry-clean", _, _) =>new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber,
                Sensitive = ((p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!)==true) ||
                                  (p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive") ? "Y" : "N"},
            _ => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber
            }
        };


        public async Task<string?> CreateParameters([FromBody] RequiredInfoDto infoDto,string ItemName)
        {

            // 1. 计算最大值
            string? largestVarName = await _helper.MaxCompositionType(infoDto.fiberComposition!)!;
            string menuName = infoDto.menuName!;
            if (menuName == null) { return null; }
            // 2. 根据 Menu/Item 组合查表
            return GetParameter(menuName, ItemName, largestVarName);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Menu, string Item, string? Lv), string?> _map = new()
        {
            [("Knit(Mango)", "Pilling Resistance", "Vegetable")] = "Cycle: 14400r",
            [("Knit(Mango)", "Pilling Resistance", "Man-made")] = "Cycle: 10800r",
            [("Knit(Mango)", "Pilling Resistance", "Synthetic")] = "Cycle: 10800r",
            [("Knit(Mango)", "Pilling Resistance", "Animal")] = "Cycle: 7200r",
            [("Knit(Mango)", "Pilling Resistance", null)] = null,
            [("Knit(Mango)", "CF to Light", null)] = "Light: L-5",

            [("Woven(Mango)", "Water Resistance-Hydrostatic Pressure", null)] = "Pressure: 90cm H2O",
            [("Woven(Mango)", "CF to Light", null)] = "Light: L-5",
            [("Woven(Mango)", "Snagging Resistance", null)] = "Cycle: 7600r",
            [("Woven(Mango)", "Pilling Resistance", null)] = "Cycle: 72000r",
            [("Woven(Mango)", "Abrasion Resistance", null)] = "Load: 9KPa,Cycle: 15000r",
        };

        private static string? GetParameter(string menu, string item, string? lv)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((menu, item, lv), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((menu, item, null), out var fallback)) return fallback;

            return null!;
        }

    }
}
