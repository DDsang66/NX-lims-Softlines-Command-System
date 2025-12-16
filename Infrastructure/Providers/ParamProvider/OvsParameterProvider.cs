using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider
{
    public class OvsParameterProvider
    {
        private readonly FiberContentHelper _helper;

        public OvsParameterProvider(FiberContentHelper helper)
        {
            _helper = helper;
        }
        //仅仅用于修改对应ItemName中的Parameter
        public WetParameterIso CreateWetParameters(ParamsInput p) => (p.ItemName, p.WashingProcedure, p.DCProcedure) switch
        {
            ("Colour Fastness to Washing", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Temperature = WashingProcedureBuilder(p.WashingProcedure,p.MenuName!).Contains("4") == true ? "40" : "60",
                Program = WashingProcedureBuilder(p.WashingProcedure,p.MenuName!).Contains("4") == true ? "A2S" : "C2S",
                SteelBallNum = BallNumberBuilder(p.MenuName!,p.FiberContent!),
                SteelBallType = "Steel Ball"
            },
            ("Dimensional Stability to Washing", _, _) => new WetParameterIso 
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                WashingProcedure =WashingProcedureBuilder(p.WashingProcedure,p.MenuName!),
                Temperature = WashingProcedureBuilder(p.WashingProcedure,p.MenuName!).Contains("4") == true ? "40" : "60",
                DryProcedure = DryProcedureBuilder(p.DryProcedure,p.MenuName!,p.FiberContent!),
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Accelerated Ageing(Stroage) Test", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Program = "7 days",
                WashingProcedure = "90%RH",
                Temperature = "70°C",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
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
            string? condition = null;
            string? condition1 = null;
            switch (ItemName) 
            {
                case"Colour Fastness to Rubbing on Leather":
                    if (infoDto.menuName == "E") condition = "E";
                    else if (infoDto.menuName == "LG") condition = "LG";
                    else  condition = "KL&KP";
                    break;
                case "Colour Fastness to Light":
                    var sampleTypeMap = new Dictionary<string, string>{ 
                        { "Turquoise", "Turquoise" },  { "Brilliant Color", "Brilliant" },   
                        { "Fluo", "Fluo" }, { "Lining", "Lining" }, { "Sweatband", "Sweatband" }
                    };
                    var lightFastnessMap = new Dictionary<string, string> 
                    { 
                        { "L", "L-3" }, { "L-SKI", "L-3" } , { "L-Act", "L-3" }, { "PP", "L-3" }
                        , { "N", "L-5" }, { "O", "L-5" }, { "P", "L-3" }, { "T", "L-5" }
                        , { "U", "L-5" }, { "V", "L-5" }, { "Z", "L-5" }, { "HTL-N-Bed Sheet", "L-5" }
                        , { "HTL-T-Bathrobe&Towel", "L-5" }, { "HTL-P-TableClothes", "L-5" }, { "HTL-S-SPA&Sea Towel", "L-5" }, { "UPT-T", "L-5" }
                    };
                    condition1 = sampleTypeMap.Keys
                        .FirstOrDefault(key => infoDto.sampleDescription!.Contains(key));

                    condition = lightFastnessMap.FirstOrDefault(kvp =>
                        infoDto.menuName!.Contains(kvp.Key)).Value;

                    return condition != null ? lightFastnessMap[condition] : "L-4";
                case "Colour Fastness to Chlorinated Water":
                    if (infoDto.sampleDescription!.Contains("Swimwear")) condition = "50";
                    else if (infoDto.menuName!.Contains("LG")) condition = "50";
                    else condition = "20";
                    break;
                case "Water Permeability/Hydrostatic Head":
                    condition = infoDto.menuName switch
                    {
                        "A-Act" => "3000",
                        "A" when infoDto.sampleDescription!.Contains("Garment") => "1800",
                        "A-SKI wear" or "I-SKI wear" => infoDto.sampleDescription!.Contains("With Membrane") ? "2000" : "5000",
                        _ => "N/A"
                    };
                    condition1 = infoDto.menuName switch
                    {
                        "A-SKI wear" =>"5 Cycle",
                        "I-SKI wear" => "3 Cycle",
                        _ => null
                    };
                    break;
            }

            return GetParameter(ItemName, condition, condition1);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Item, string? Condition, string? Condition1), string?> _map = new()
        {
            [("Colour Fastness to Rubbing on Leather", "E", null)] = "Dry: 50 Cycles；Wet: 20 Cycles；Sweat: 50 Cycles；",
            [("Colour Fastness to Rubbing on Leather", "LG", null)] = "Dry: 150 Cycles；Wet: 50 Cycles；",
            [("Colour Fastness to Rubbing on Leather", "KL&KP", null)] = "Wet & Dry: 50 Cycles",
            [("Colour Fastness to Light", "L-3", null)] = "L-3",
            [("Colour Fastness to Light", "L-4", null)] = "L-4",
            [("Colour Fastness to Light", "L-5", "Turquoise")] = "L-3",
            [("Colour Fastness to Light", "L-5", "Brilliant")] = "L-3",
            [("Colour Fastness to Light", "L-5", "Fluo")] = "L-3",
            [("Colour Fastness to Light", "L-5", "Sweatband")] = "L-3",
            [("Colour Fastness to Light", "L-5", null)] = "L-5",
            [("Colour Fastness to Chlorinated Water", "50", null)] = "50ppm",
            [("Colour Fastness to Chlorinated Water", "20", null)] = "20ppm",
            [("Dimensional Stability to Dry-Cleaning", null, null)] = "Commercial Cycle",
            [("Appearance after Washing/Dry-Cleaning", null, null)] = "Same Test Method as Dimensional Stability",
            [("Calculation of Color Differences", null, null)] = "∆E - D65 and TL84",
            [("Movement after Washing", null, null)] = "TM179 Option1, Test Method Same as Dimensional Stability",
            [("Water Permeability/Hydrostatic Head","1800",null)]="Press: 1800mmH2O，Original Sample",
            [("Water Permeability/Hydrostatic Head", "2000", "3 Cycle")] = "Press: 2000mmH2O，After 3Cycles",
            [("Water Permeability/Hydrostatic Head", "2000", "5 Cycle")] = "Press: 2000mmH2O，After 5 Cycles",
            [("Water Permeability/Hydrostatic Head", "3000", null)] = "Press: 3000mmH2O，After 5 Cycles",
            [("Water Permeability/Hydrostatic Head", "5000", "3 Cycle")] = "Press: 5000mmH2O，After 3 Cycles",
            [("Water Permeability/Hydrostatic Head", "5000", "5 Cycle")] = "Press: 5000mmH2O，After 5 Cycles",
            [("Water Permeability/Hydrostatic Head", "N/A", null)] = "N/A",

        };

        private static string? GetParameter(string Item, string? Condition, string? Condition1)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((Item, Condition, Condition1), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((Item, Condition, null), out var fallback)) return fallback;

            return null!;
        }


        private string WashingProcedureBuilder(string? WashingProcedure,string Menuname) 
        {
            return "6N";
        }
        private int BallNumberBuilder(string Menuname, List<FiberDto> fiberComposition)
        {
            return 10;
        }

        private string DryProcedureBuilder(string? DryProcedure, string Menuname, List<FiberDto> fiberComposition)
        {
            return "Tumble Dry";
        }

    }
}
