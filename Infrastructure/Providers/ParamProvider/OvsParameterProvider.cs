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
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Temperature = WashingProcedureBuilder(p.WashingProcedure,p.MenuName!,p.FiberContent!).Contains("6") == true ? "60" : "40",
                Program = WashingProcedureBuilder(p.WashingProcedure,p.MenuName!,p.FiberContent!).Contains("6") == true ? "C2S" : "A2S",
                SteelBallNum = BallNumberBuilder(p.MenuName!,p.FiberContent!),
                SteelBallType = "Steel Ball"
            },
            ("Dimensional Stability to Washing", _, _) => new WetParameterIso 
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure =WashingProcedureBuilder(p.WashingProcedure,p.MenuName!, p.FiberContent!),
                Temperature = WashingProcedureBuilder(p.WashingProcedure, p.MenuName!, p.FiberContent!).Contains("6") ? "60" 
                : WashingProcedureBuilder(p.WashingProcedure, p.MenuName!, p.FiberContent!).Contains("3") ? "30"
                :"40",
                DryProcedure = DryProcedureBuilder(p.DryProcedure,p.MenuName!,p.FiberContent!),
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Dimensional Stability to Dry-Cleaning", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null
            },
            ("Accelerated Ageing(Stroage) Test", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = "7 days",
                WashingProcedure = "90%RH",
                Temperature = "70°C",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Moisture Management", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = "3N",
                Temperature = "30",
                DryProcedure = "Line Dry",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Pilling Resistance", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = PillingWashingProcedureBuilder(p.WashingProcedure,p.FiberContent!),
                Temperature = p.WashingProcedure!.Contains("4")?"40":"60",
                DryProcedure = _helper.IsCompositionSourceExist("Animal",p.FiberContent!)>0?"Flat Dry":p.DryProcedure,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Bursting Strength", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = _helper.CompositionRate(p.FiberContent!, "Silk") > 0 ? PillingWashingProcedureBuilder(p.WashingProcedure, p.FiberContent!) : p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : p.WashingProcedure!.Contains("6") ? "60" : "30",
                DryProcedure = _helper.IsCompositionSourceExist("Animal", p.FiberContent!) > 0 ? "Flat Dry" : p.DryProcedure,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
    : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
    : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Seam Slippage", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = _helper.CompositionRate(p.FiberContent!, "Silk") > 0 ? PillingWashingProcedureBuilder(p.WashingProcedure,p.FiberContent!) : p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : p.WashingProcedure!.Contains("6")?"60":"30",
                DryProcedure = _helper.IsCompositionSourceExist("Animal", p.FiberContent!) > 0 ? "Flat Dry" : p.DryProcedure,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
    : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
    : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Vertical Wicking", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = "3N",
                Temperature = "30",
                DryProcedure = "Line Dry",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
: _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
: "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = "3 Cycles",
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
                case "Water Repellency":
                    condition = infoDto.menuName switch
                    {
                        "A" => "1 Cycle",
                        "A1" => "1 Cycle",
                        "E" => "1 Cycle",
                        "UM-Umbrellas" => "Original Sample",
                        "A-SKI wear" or "A-Act" =>"5 Cycle",
                        _ => "N/A"
                    };
                    break;
                case "Air Permeability":
                    condition = infoDto.menuName switch
                    {
                        "I-SKI wear" => "3 Cycle",
                        _ => "5 Cycle"
                    };
                    break;
                case "Absorbency":
                    if(infoDto.menuName=="HTL-Y-Slipper")condition = "Original Sample";
                    else condition = "1 Cycle";
                    break;
                case "Pilling Resistance":
                    if (standard.Contains("12945-1")) condition = "ICI";
                    else if (standard.Contains("12945-2")) condition = "Martindale";
                    break;
                case "Abrasion Resistance":
                    condition = infoDto.menuName switch
                    {
                        "E" => "3",
                        "UPF-T" => "12",
                        _ => "9"
                    };
                    condition1 = infoDto.menuName switch
                    {
                        "J-SKI wear"or"J-Act"or"J" => "15000",
                        "I-SKI wear" or "A-SKI wear" or "C" => "30000",
                        "UPF-T" or "A-Act" or "A" or "A1" or "B" or "F" or "P" or "T" or "U" or "HTL-S-SPA&Sea Towel" => "20000",
                        _ => "10000"
                    };
                    break;
                case "Bursting Strength":
                    if (!infoDto.sampleDescription!.Contains("Knit")) condition = "N/A";
                    if(_helper.CompositionRate(infoDto.fiberComposition!,"Silk")>0) condition1 = "After Wash";
                    break;
                case "Seam Slippage":
                    if (!infoDto.sampleDescription!.Contains("Woven")) condition = "N/A";
                    if (_helper.CompositionRate(infoDto.fiberComposition!, "Silk") > 0) condition1 = "After Wash";
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
            [("Water Permeability/Hydrostatic Head", "1800", null)] = "Press: 1800mmH2O，Original Sample",
            [("Water Permeability/Hydrostatic Head", "2000", "3 Cycle")] = "Press: 2000mmH2O，After 3Cycles",
            [("Water Permeability/Hydrostatic Head", "2000", "5 Cycle")] = "Press: 2000mmH2O，After 5 Cycles",
            [("Water Permeability/Hydrostatic Head", "3000", null)] = "Press: 3000mmH2O，After 5 Cycles",
            [("Water Permeability/Hydrostatic Head", "5000", "3 Cycle")] = "Press: 5000mmH2O，After 3 Cycles",
            [("Water Permeability/Hydrostatic Head", "5000", "5 Cycle")] = "Press: 5000mmH2O，After 5 Cycles",
            [("Water Repellency", "1 Cycle", null)] = "After 1 Cycle；4N@40°C. ",
            [("Water Repellency", "5 Cycle", null)] = "After 5 Cycle；4N@40°C. ",
            [("Water Repellency", "Original Sample", null)] = "Original Sample",
            [("Air Permeability", "1 Cycle", null)] = "After 1 Cycle；4N@40°C. ",
            [("Air Permeability", "5 Cycle", null)] = "After 5 Cycle；4N@40°C. ",
            [("Absorbency","Original Sample",null)] = "Original Sample",
            [("Absorbency", "1 Cycle", null)] = "After 1 Cycle；4N@40°C. ",
            [("Moisture Management", null, null)] = "After 1 Cycle",
            [("Pilling Resistance","ICI",null)]= "Evaluation at 7.200 and 10.800 revs",
            [("Pilling Resistance", "Martindale", null)] = "Tex-tex Evaluation at 500, 1000 and 2000 revs",
            [("Abrasion Resistance", "3", null)] = "Load: 3KPa；CC ≥ 3-4 at 10000 revs；No noticeable changes at 20000 revs ",
            [("Abrasion Resistance", "12", null)] = "Load:12KPa；Evaluation at 20000 revs；CC ≥ 3-4 at 3.000 revs",
            [("Abrasion Resistance", "9", "10000")] = "Load: 9KPa；Evaluation at 10000 revs；CC ≥ 3-4 at 3.000 revs",
            [("Abrasion Resistance", "9", "15000")] = "Load: 9KPa；Evaluation at 15000 revs；CC ≥ 3-4 at 3.000 revs",
            [("Abrasion Resistance", "9", "20000")] = "Load: 9KPa；Evaluation at 20000 revs；CC ≥ 3-4 at 3.000 revs",
            [("Abrasion Resistance", "9", "30000")] = "Load: 9KPa；Evaluation at 30000 revs；CC ≥ 3-4 at 3.000 revs",
            [("Bursting Strength", "N/A", null)] = "N/A",
            [("Bursting Strength", null, "After Wash")] = "After 1 Hand Cycle",
            [("Bursting Strength", null, "Unit Weight")] = "Need additional unit weight",
            [("Seam Slippage", "N/A", null)] = "N/A",
            [("Seam Slippage", null, "After Wash")] = "After 1 Hand Cycle",
            [("Seam Slippage", null, "Unit Weight")] = "Need additional unit weight",
            [("Stretch & Recovery", null,null)]= "Stretch: ≥ 15%/Residual Extension: ≤ 5%",
            [("Tensile Strength", null, null)] = "Need additional unit weight",
            [("Tear Strength", "", null)] = "Need additional unit weight",
            [("Bursting Strength", "N/A", null)] = "N/A",
            [("Drying Rate", null, null)] = "After 30 mins",
        };

        private static string? GetParameter(string Item, string? Condition, string? Condition1)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((Item, Condition, Condition1), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((Item, Condition, null), out var fallback)) return fallback;

            return null!;
        }


        private string WashingProcedureBuilder(string? WashingProcedure, string Menuname, List<FiberDto> fiberComposition)
        {
            if (_helper.IsCompositionSourceExist("Animal", fiberComposition) > 0) return "3G";
            else if (Menuname == "PP-Period Panties") return "5A";
            else if (Menuname == "O" || Menuname == "P" || Menuname == "T"
                || Menuname == "HTL-P-TableClothes" || Menuname == "HTL-N-Bed Sheet"
                || Menuname == "HTL-T-Bathrobe&Towel" || Menuname == "HTL-S-SPA&Sea Towel") return "6N";
            else return "4N";
        }
        private string PillingWashingProcedureBuilder(string? WashingProcedure, List<FiberDto> fiberComposition)
        {
            var aniRate = _helper.IsCompositionSourceExist("Animal", fiberComposition);
            if (aniRate == 0)
            {
                if (WashingProcedure!.Contains("4")) return "4N";
                else if (WashingProcedure!.Contains("3")) return "3N";
                else return "6N";
            }
            else if (aniRate > 0&& WashingProcedure!.Contains("4")) return "4H";
            else if (aniRate > 0 && WashingProcedure!.Contains("3")) return "3H";
            else return "4N";
        }

        private int BallNumberBuilder(string Menuname, List<FiberDto> fiberComposition)
        {
            if (Menuname == "O" || Menuname == "P" || Menuname == "T"
                 || Menuname == "HTL-P-TableClothes" || Menuname == "HTL-N-Bed Sheet"
                 || Menuname == "HTL-T-Bathrobe&Towel" || Menuname == "HTL-S-SPA&Sea Towel") return 25;
            else 
            {
                var ballNum = _helper.IsCompositionExist("Animal",fiberComposition!) == true ? 0 : 10;
                return ballNum;
            }
        }

        private string DryProcedureBuilder(string? DryProcedure, string Menuname, List<FiberDto> fiberComposition)
        {
            if (_helper.IsCompositionSourceExist("Animal", fiberComposition) > 0) return "Flat Dry";
            else return "Tumble Dry";
        }

    }
}
