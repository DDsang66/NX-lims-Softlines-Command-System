using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;


namespace NX_lims_Softlines_Command_System.Infrastructure.Providers
{
    public class PrimarkParameterProvider
    {
        private readonly FiberContentHelper _helper;

        public PrimarkParameterProvider(FiberContentHelper helper)
        {
            _helper = helper;
        }
        //仅仅用于修改对应ItemName中的Parameter
        public WetParameterIso CreateWetParameters(ParamsInput p) => (p.ItemName, p.WashingProcedure, p.DCProcedure,p.MenuName) switch
        {
            ("CF to Washing", "4H" or "3M" or "3G" or "3H", _, "PTC03" or "PTC04" or "PTC24") => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Temperature = p.WashingProcedure!.Contains("3") == true ? "30" : "40",
                Program = p.WashingProcedure.Contains("3") == true ? "ref A2S" : "A2S",
                SteelBallNum = 0,
                SpecialCareInstruction = (p.SampleDescription!.Contains("White")||p.SampleDescription.Contains("Cream")) == true ? "N/A" : null
            },
            ("CF to Washing", "4N" or "4M" or "4G" or "3N", _, "PTC03" or "PTC04" or "PTC24") => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Temperature = p.WashingProcedure!.Contains("3") == true ? "30" : "40",
                Program = p.WashingProcedure.Contains("3") == true ? "ref A2S" : "A2S",
                SteelBallNum = _helper.IsCompositionExist("Animal", p.FiberContent!) == true ? 0 : 10,
                SpecialCareInstruction = (p.SampleDescription!.Contains("White") || p.SampleDescription.Contains("Cream")) == true ? "N/A" : null
            },
            ("CF to Washing", _, _ , _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Temperature = "40",
                Program = "A2S",
                SteelBallNum = _helper.IsCompositionExist("Animal", p.FiberContent!) == true ? 0 : 10,
                SpecialCareInstruction = (p.SampleDescription!.Contains("White") || p.SampleDescription.Contains("Cream")) == true ? "N/A" : null
            },
            ("Absorbency", "3H"or "4H", _ , _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                WashingProcedure = p.WashingProcedure,
                Temperature =
                p.WashingProcedure!.Contains("3") ? "80" : "105",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Absorbency", _ , _ , _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("Cold") ? "80"
                : p.WashingProcedure.Contains("Warm") ? "105"
                : p.WashingProcedure.Contains("Hot") ? "120"
                : "140",
                Bleach = p.WashingProcedure!.Contains("Normal") ? "Normal"
                : p.WashingProcedure.Contains("Gentle") ? "Gentle"
                : p.WashingProcedure.Contains("Permanent Press") ? "Permanent"
                : "",
                //Cycle，程度暂时用Bleach字段代替
                DryCleanProcedure = DryConditionHelper(p.DryProcedure!),
                //DryCondition，暂时用干洗字段代替
                AfterWash = "20",
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Colour Fastness to Hot Pressing", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Temperature = HotPressingHelper(p.IronMethod,p.MenuName!),
                IronMethod = p.IronMethod ?? null,
            },
            ("Dimensional and Bra Wire Casing Stability", _, _, _)=> new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = "4H",
                Temperature = "40",
                SpecialCareInstruction = p.Sci,
                Iron = p.Iron??null,
                IronMethod= p.IronMethod ?? null,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                DryCleanProcedure = p.DCProcedure,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
            },
            ("Martindale Pilling", _, _, _) => new WetParameterIso 
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                SpecialCareInstruction = p.Sci,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                DryCleanProcedure = p.DCProcedure,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
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
            string? largestVarName = await _helper.MaxCompositionType(infoDto.fiberComposition!)!;
            string? Condition = null;
            switch (ItemName) 
            {
                case "Accelerotor":
                    if (infoDto.sampleDescription!.Contains("Velvet")) Condition = "3min";
                    else if (infoDto.sampleDescription!.Contains("Corurcy") || infoDto.sampleDescription.Contains("Velour")) Condition = "5min";
                    else Condition = null; 
                    break;
                case "Colour Fastness to Chlorinated Water":
                    if (infoDto.sampleDescription!.Contains("Swimwear")) Condition = "50";
                    else if (infoDto.sampleDescription!.Contains("Beachwear")) Condition = "20";
                    else Condition = "20";
                    break;
                case "Colour Fastness to Light":
                    Condition = "L-4";
                    if (infoDto.sampleDescription!.Contains("Neon") && infoDto.menuName != "PTC01") Condition = "L-3";
                    break;
                case "Colour Fastness to Water":
                    if (infoDto.sampleDescription!.Contains("White") || infoDto.sampleDescription.Contains("Cream")) Condition = "N/A";
                    else Condition = null;
                    break;
                case "Martindale Abrasion":
                    if (infoDto.menuName == "PTC03" || infoDto.menuName == "PTC04" || infoDto.menuName == "PTC37") Condition = "no change shade";
                    else Condition = null;
                    break;
                case "Martindale Pilling":
                    if (infoDto.sampleDescription!.Contains("Woven")) Condition = "Woven";
                    else if (infoDto.sampleDescription!.Contains("Knit")) Condition = "Knit";
                    else Condition = null;
                    break;
            }

            return GetParameter(Condition, ItemName, largestVarName);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string? Condition, string Item, string? Lv), string?> _map = new()
        {
            [(null, "Abrasion of Knitted Footwear Garments - Modified Martindale",null)] = "Cycle: 8000r",
            [("3min", "Accelerotor", null)] = "Time:3min,Cycle: 2000R.P.M",
            [("5min", "Accelerotor", null)] = "Time:5min,Cycle: 2000R.P.M",
            [(null, "Bursting Strength", null)] = "Diameter: 79.8mm,Square:50cm²",
            [("20", "Colour Fastness to Chlorinated Water", null)] = "20mg/L",
            [("50", "Colour Fastness to Chlorinated Water", null)] = "50mg/L",
            [(null, "Colour Fastness to Dry Cleaning", null)] = "Multi-Fibre Type:SDC",
            [("L-3", "Colour Fastness to Light", null)] = "L-3",
            [("L-4", "Colour Fastness to Light", null)] = "L-4",
            [("N/A", "Colour Fastness to Water", null)] = "N/A",
            [(null, "Colour Fastness to Washing", null)] = "Multi-Fibre Type:LyoW",
            [(null, "Colour Fastness to Water", null)] = "Multi-Fibre Type:LyoW",
            [(null, "Martindale Abrasion", null)] = "9KPa,Shade Change @ 5000",
            [("no change shade", "Martindale Abrasion", null)] = "9KPa",
            [("Woven", "Martindale Abrasion", null)] = "Cycle:2000 revs",
            [("Knit", "Martindale Abrasion", null)] = "Cycle:500 revs",
        };

        private static string? GetParameter(string? Condition, string item, string? lv)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((Condition, item, lv), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((Condition, item, null), out var fallback)) return fallback;

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

        private string? HotPressingHelper(string? IronMethod,string MenuName)
        {
            string? Temperature = null;
            Temperature = IronMethod!.Contains("Cool") ? "100"
                : IronMethod!.Contains("Warm") ? "150"
                : IronMethod!.Contains("Hot") ? "200"
                : "/";
            if ((MenuName == "PTC35"|| MenuName=="PTC36") && Temperature=="100") Temperature = "110";
            return Temperature;
        }

    }
}