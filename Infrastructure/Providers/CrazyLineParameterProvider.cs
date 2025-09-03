using NX_lims_Softlines_Command_System.Models;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;


namespace NX_lims_Softlines_Command_System.Infrastructure.Providers
{
    public class CrazyLineParameterProvider
    {
        private readonly FiberContentHelper _helper;

        public CrazyLineParameterProvider(FiberContentHelper helper)
        {
            _helper = helper;
        }
        //仅仅用于修改对应ItemName中的Parameter
        public WetParameterAatcc CreateWetParameters(ParamsInput p) => (p.ItemName, p.WashingProcedure, p.DCProcedure) switch
        {
            ("CF to Washing", "Hand Wash Cold" or "Hand Wash", _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure.Contains("Cold") == true ? "88" : "105",
                Program = p.WashingProcedure.Contains("Cold") == true ? "1B" : "1A",
                SteelBallNum = 10,
                AfterWash = p.sampleDescription!.Contains("1 Wash") == true ? 1 : 3,
            },
            ("CF to Washing", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("Cold") ? "85"
                : p.WashingProcedure!.Contains("Warm") ? "105"
                : "0",
                Program = (p.WashingProcedure!.Contains("Cold") || p.WashingProcedure!.Contains("Warm")) ? "ref 2A" : "",
                SteelBallNum = (p.WashingProcedure!.Contains("Cold") || p.WashingProcedure!.Contains("Warm")) ? 50 : 0
            },
            ("DS to Washing", "Hand Wash Cold" or "Hand Wash", _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber,
                DryProcedure = p.DryProcedure,
                WashingProcedure = p.WashingProcedure,
                Temperature =
                p.WashingProcedure!.Contains("Cold") ? "80" : "105",
                AfterWash = p.sampleDescription!.Contains("1 Wash") == true ? 1 : 3,
            },
            ("DS to Washing", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber,
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
                AfterWash = p.sampleDescription!.Contains("1 Wash") == true ? 1 : 3,
            },
            ("DS to Dry-clean", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber,
                Sensitive = ((p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true) ||
                                  (p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive") ? "Y" : "N" 
            },
            ("Spriality/Skewing", "Hand Wash Cold" or "Hand Wash", _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber,
                DryProcedure = p.DryProcedure,
                WashingProcedure = p.WashingProcedure,
                Temperature =
                p.WashingProcedure!.Contains("Cold") ? "80" : "105",
                AfterWash = p.sampleDescription!.Contains("1 Wash") == true ? 1 : 3,
            },
            ("Spriality/Skewing", _, _) => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber,
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
                AfterWash = p.sampleDescription!.Contains("1 Wash") == true ? 1 : 3,
            },
            _ => new WetParameterAatcc
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber
            }
        };

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

        public async Task<string?> CreateParameters([FromBody] RequiredInfoDto infoDto,string ItemName)
        {

            // 1. 计算最大值
            string menuName = infoDto.menuName!;
            if (menuName == null) { return null; }
            // 2. 根据 Menu/Item 组合查表
            return GetParameter(menuName, ItemName);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Menu, string Item), string?> _map = new()
        {
            [("Knit(CrazyLine)", "Pilling Resistance")] = "Time: 30min",
            [("Knit(CrazyLine)", "CF to Light")] = "Light: 20 AFU",
            [("Knit(CrazyLine)", "Snagging Resistance")] = "Cycle: 600 Revolutions",
            [("Knit(CrazyLine)", "Spriality/Skewing")] = "Same as DS to Washing",

            [("Woven(CrazyLine)", "Snagging Resistance")] = "Cycle: 600 Revolutions",
            [("Woven(CrazyLine)", "CF to Light")] = "Light: 20 AFU",
            [("Woven(CrazyLine)", "Pilling Resistance")] = "Time: 30min",
            [("Woven(CrazyLine)", "Spriality/Skewing")] = "Same as DS to Washing",
        };

        private static string? GetParameter(string menu, string item)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((menu, item), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((menu, item), out var fallback)) return fallback;

            return null!;
        }

    }
}
