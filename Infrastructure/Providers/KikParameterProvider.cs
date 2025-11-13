using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using DocumentFormat.OpenXml.Presentation;


namespace NX_lims_Softlines_Command_System.Infrastructure.Providers
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
                ReportNumber = p.OrderNumber!
            }
        };


        public async Task<string?> CreateParameters([FromBody] RequiredInfoDto infoDto, string ItemName)
        {

            // 1. 计算最大值
            string? Condition = null;
            return GetParameter(ItemName, Condition);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Item, string? Lv), string?> _map = new()
        {
            [("Appearance", null)] = "Same as Dimensional Stability to Washing",
            [("Air Permeability", null)] = "Area: 20cm²",
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
