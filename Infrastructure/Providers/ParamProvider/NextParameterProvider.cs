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
            ("Fastness to Washing", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Temperature = "50",
                Program = "B2S",
                SteelBallNum = _helper.IsCompositionExist("Animal", p.FiberContent!) == true ? 0 : 25
            },
            ("Cross Staining to Washing", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Temperature = "50",
                Program = "B2S",
                SteelBallNum = _helper.IsCompositionExist("Animal", p.FiberContent!) == true ? 0 : 25
            },
            ("Print Durability", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure ="2A",
                DryProcedure = "Tumble Dry for 90 min",
                Temperature = "60",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Embellishment Durability (Childrenswear)", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = "2A",
                DryProcedure = "Tumble Dry for 90 min",
                Temperature = "60",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Embellishment Durability (General)", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = WashingProcedureTranslationHelper(p.WashingProcedure!),
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Foil Durability", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = WashingProcedureTranslationHelper(p.WashingProcedure!),
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Appearance Assessment after Wash", _, _) => new WetParameterIso 
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure!,
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                Detergent = "25g Print Test Durability Detergent",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Appearance Assessment after Dry Clean",_,_) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null
            },
            ("Polar Fleece Assessment", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = "5A",
                DryProcedure = "Tumble Dry Height",
                Temperature = "40",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
    : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
    : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Stability to Washing", _, _) => new WetParameterIso 
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = WashingProcedureHelper(p.FiberContent!,p.SampleDescription!,p.WashingProcedure),
                DryProcedure = DryProcedureHelper(p.FiberContent!, p.SampleDescription!, p.DryProcedure),
                Temperature = "40",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                Detergent = "ECE(A)+ Sodium perborate",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Spirality",_,_) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = WashingProcedureHelper(p.FiberContent!, p.SampleDescription!, p.WashingProcedure),
                DryProcedure = DryProcedureHelper(p.FiberContent!, p.SampleDescription!, p.DryProcedure),
                Temperature = "40",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Stability to Dry Cleaning",_,_) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null
            },
            ("Assessment of Easy to Iron Fabrics",_,_)=> new WetParameterIso 
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = WashingProcedureHelper(p.FiberContent!, p.SampleDescription!, p.WashingProcedure),
                DryProcedure = DryProcedureHelper(p.FiberContent!, p.SampleDescription!, p.DryProcedure),
                Temperature = "40",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = _helper.CompositionRate(p.FiberContent!, "Cotton") == 100 ? "190±10" 
                : (_helper.CompositionRate(p.FiberContent!,"Polyester") + _helper.CompositionRate(p.FiberContent!, "Cotton")) >90? "150±10"
                : "130±10",
                IronMethod = p.IronMethod ?? null,

                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" 
                :string.IsNullOrWhiteSpace(p.DCProcedure) ?null : "N",

            },
            ("Spray Rating", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = WashingProcedureHelper(p.FiberContent!, p.SampleDescription!, p.WashingProcedure),
                DryProcedure = DryProcedureHelper(p.FiberContent!, p.SampleDescription!, p.DryProcedure),
                Temperature = "40",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                SpecialCareInstruction = p.Sci ?? null,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = _helper.CompositionRate(p.FiberContent!, "Cotton") == 100 ? "190±10"
                : (_helper.CompositionRate(p.FiberContent!, "Polyester") + _helper.CompositionRate(p.FiberContent!, "Cotton")) > 90 ? "150±10"
                : "130±10",
                IronMethod = p.IronMethod ?? null,

                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y"
                : p.DCProcedure == "" ? null : "N",

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
            string? Condition1 = null;
            if (ItemName == "Pilling Resistance")
            {
                Condition = infoDto.sampleDescription!.Contains("Knit") ? "Knit" : "Woven";
            }
            else if (ItemName == "Fastness to Rubbing")
            {
                Condition = infoDto.sampleDescription!.Contains("dry rubbing only") ? "dry" : infoDto.sampleDescription!.Contains("wet rubbing only") ? "wet" : "both";
            }
            else if (ItemName == "Swiss Pilling")
            {
                Condition = infoDto.sampleDescription!.Contains("Home") ? "Home" : "Apparel";
                Condition1 = infoDto.sampleDescription!.Contains("Knit") ? "Knit" : "Woven";
            }
            else if (ItemName == "Martindale Abrasion")
            {
                if (_helper.CompositionRate(infoDto.fiberComposition!, "Elastane") > 10
                    ||infoDto.sampleDescription!.Contains("Dress")
                    ||infoDto.sampleDescription.Contains("Blouse")
                    || infoDto.sampleDescription.Contains("Tailoring")) Condition = "Stretch";
            }
            else if (ItemName == "Extension and Recovery")
            {
                if (_helper.CompositionRate(infoDto.fiberComposition!, "Elastane") > 10) Condition1 = "TM21";
                else Condition = infoDto.sampleDescription!.Contains("Knit") ? "Knit" : "Woven";
            }
            else if (ItemName == "Extension and Modulus")
            {
                if (infoDto.sampleDescription!.Contains("Briefs")) Condition = "Briefs";
                else if (infoDto.sampleDescription!.Contains("Shoulder Strap")) Condition = "Shoulder Strap";
                else if (infoDto.sampleDescription!.Contains("Underarm and Underband")) Condition = "UU";
                else if (infoDto.sampleDescription!.Contains("Wide Elastics")) Condition = "Wide Elastics";
                else if (infoDto.sampleDescription!.Contains("Knit")) Condition = "Knit";
                else if (infoDto.sampleDescription!.Contains("Woven")) Condition = "Woven";
            }
            return GetParameter(ItemName, Condition,Condition1);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Item, string? Condition, string? Condition1), string?> _map = new()
        {
            [("Fastness to Light", null,null)] = "L-4",
            [("Fastness to Washing", null, null)] = "Multi-Fibre Type:LW",
            [("Cross Staining to Washing", null,null)] = "Multi-Fibre Type:LW",
            [("Fastness to Dry Cleaning", null,null)] = "Multi-Fibre Type:LW",
            [("Cross Staining to Dry Cleaning", null, null)] = "Multi-Fibre Type:LW",
            [("Fastness to Water", null, null)] = "Multi-Fibre Type:LW",
            [("Fastness to Rubbing", "dry", null)] = "dry rubbing only",
            [("Fastness to Rubbing", "wet", null)] = "wet rubbing only",
            [("Fastness to Rubbing", "both", null)] = "Both dry and wet rubbing",
            [("Cross Staining to Water", null,null)] = "Multi-Fibre Type:LW",
            [("Fastness to Chlorinated Water", null, null)] = "50mg/L",
            [("Stability to Dry Cleaning", null, null)] = "Wash Procedure: Commercial dry-cleaning",
            [("Appearance Assessment after Dry Clean", null, null)] = "Wash Procedure: Commercial dry-cleaning",
            [("Grab Strength & Seam Slippage", null,null)] = "Need additional unit weight；{≤150g/m²: 8kg；150-250g/m²: 15kg；≥250g/m²: 20kg}",
            [("Seam Slippage of Garment Seams", null, null)] = "Need additional unit weight",
            [("Tear Strength", null, null)] = "Need additional unit weight",
            [("Martindale Abrasion", "Stretch", null)] = "9KPa,Shade Change @ 5000 {≤150g/m²: 10000 rubs；150-250g/m²: 15000 rubs；≥250g/m²: 20000 rubs}",
            [("Martindale Abrasion", null, null)] = "9KPa,Shade Change @ 5000 {10000 rubs}",
            [("Abrasion Home", null, null)] = "12KPa；Cycle：20000 revs；Shade Change @ 6000&10000",
            [("Bursting Strength", null, null)] = "Diameter: 30.5mm",
            [("Pilling Resistance", "Woven",null)] = "Cycle: 18000 revs；After 1 Wash",
            [("Pilling Resistance", "Knit",null)] = "Cycle: 7200 revs；After 1 Wash",
            [("Extension and Recovery", "Woven", null)] = "Load: 4.0 kg",
            [("Extension and Recovery", "Knit", null)] = "Load: 2.0 kg",
            [("Extension and Recovery", null, "TM21")] = "Change to Method: TM21",
            [("Extension and Modulus", "Woven", null)] = "Load: 4.0kg，Modulus: 10%",
            [("Extension and Modulus", "Knit", null)] = "Load: 3.6kg，Modulus: 40%",
            [("Extension and Modulus", "Briefs", null)] = "Load: 1.5kg，Modulus: 40%",
            [("Extension and Modulus", "UU", null)] = "Load: 2.5kg，Modulus: 40%",
            [("Extension and Modulus", "Shoulder Strap", null)] = "Load: 3.6kg，Modulus: 40%",
            [("Extension and Modulus", "Wide Elastics", null)] = "Load: 2.5kg，Modulus: 40%",
            [("Bursting Strength", null, null)] = "Diameter: 30.5mm",
            [("Fastness to Saliva", null, null)] = "Multi-Fibre Type:LW",
            [("Fastness to Sea Water", null, null)] = "Multi-Fibre Type:LW",
            [("Fastness to Perspiration", null, null)] = "Multi-Fibre Type:LW",
            [("Snagging Resistance", null, null)] = "Cycle: 2000 revs",
            [("Air Permeability of Textile Fabrics", null, null)] = "Area 20cm², P: 100Pa；Before and After 1 Wash",
            [("Swiss Pilling", "Home", "Woven")] = "Load: 12KPa; {1st pair: 1000 revs；2nd pair: 2000 revs；3rd pair: 4000 revs}",
            [("Swiss Pilling", "Apparel", "Woven")] = "Load: 9KPa; {1st pair: 500 revs；2nd pair: 1000 revs；3rd pair: 2000 revs}",
            [("Swiss Pilling", "Apparel", "Knit")] = "Load: 3KPa; {1st pair: 500 revs；2nd pair: 1000 revs；3rd pair: 2000 revs}",
            [("Hydrostatic Head Test", null, null)] = "P: 5000mmH2O；Before and After 1 Wash",
            [("Moisture Management",null,null)] = "Before and After 10 Wash",
            [("Accelerotor Pile Loss", null, null)] = "Before and After 1 Wash",
        };

        private static string? GetParameter(string item, string? Condition, string? Condition1)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((item, Condition,Condition1), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((item, null,null), out var fallback)) return fallback;

            return null!;
        }



        private string? WashingProcedureHelper(List<FiberDto> fiberComposition, string sampleDescription, string? WashingProcedure)
        {
            string washingProcedure = string.Empty;
            var maxComposition = _helper.MaxComposition(fiberComposition);
            if (sampleDescription.Contains("Garment"))
            {
                if (sampleDescription.Contains("Swimwear"))
                {
                    if (sampleDescription.Contains("Embellished")) washingProcedure = "SHW";
                    else washingProcedure = "5A";
                }
                else if (sampleDescription.Contains("Cap")
                    || sampleDescription.Contains("Gloves")
                    || sampleDescription.Contains("Socks"))
                {
                    washingProcedure = "7A";
                }
                else if (maxComposition == "Silk"
                    || maxComposition == "Wool"
                    || maxComposition == "Mohair")
                {
                    if (!string.IsNullOrEmpty(WashingProcedure)&&WashingProcedure.Contains("H")) washingProcedure = "SHW";
                    else washingProcedure = "7A";
                }
                else WashingProcedure = "7A";

            }
            else if (sampleDescription.Contains("Fabric") || sampleDescription.Contains("Mockup")) washingProcedure = "5A";
            else washingProcedure = "5A";
            return washingProcedure;
        }

        #region
        //private string? DryProcedureHelper1(List<FiberDto> fiberComposition, string sampleDescription,string? dryProcedure)
        //{
        //    string DryProcedure = string.Empty;
        //    var maxComposition = _helper.MaxComposition(fiberComposition);
        //    var descComposition = _helper.IsCompositionDescExist("Regenerated Cellulose", fiberComposition);
        //    var isSyntheticExist = _helper.IsCompositionSourceExist("Synthetic", fiberComposition) >= 51;

        //    if (sampleDescription.Contains("Woven") && sampleDescription.Contains("Fabric")) 
        //    {
        //        if (maxComposition == "Cotton" || isSyntheticExist || maxComposition == "Silk") DryProcedure = "Tumble Dry";
        //        else if (maxComposition == "Linen") DryProcedure = "Flat Dry";
        //        else if (descComposition) DryProcedure = "Line Dry";
        //        else if (maxComposition == "Wool")
        //        {
        //            if (dryProcedure.Contains("Tumble")) DryProcedure = "Tumble Dry";
        //            else DryProcedure = "Line Dry";
        //        }
        //        else if (sampleDescription.Contains("Lining") || sampleDescription.Contains("Pocket"))
        //        {
        //            if (maxComposition == "Acetate" || maxComposition == "Silk" || maxComposition == "Viscose" || maxComposition == "Acrylic") DryProcedure = "Flat Dry";
        //            else DryProcedure = "Tumble Dry";
        //        }
        //    }

        //    if (sampleDescription.Contains("Knit") && sampleDescription.Contains("Fabric")&&sampleDescription.Contains("Weft")) 
        //    {
        //        if(maxComposition=="Cotton" || isSyntheticExist) DryProcedure = "Tumble Dry";
        //        else if (maxComposition == "Silk"||maxComposition=="Wool" || descComposition || maxComposition == "Acrylic") DryProcedure = "Line Dry";
        //    }
        //    else if (sampleDescription.Contains("Knit") && sampleDescription.Contains("Fabric") && sampleDescription.Contains("Warp"))
        //    {
        //        if (sampleDescription.Contains("Stretch")) DryProcedure = "Line Dry";
        //        else DryProcedure = "Tumble Dry";
        //    }

        //    if (sampleDescription.Contains("Garment") || sampleDescription.Contains("Knitwear")) 
        //    {
        //        if (sampleDescription.Contains("Childrenswear")) DryProcedure = "Tumble Dry";
        //        else DryProcedure = "Flat Dry";
        //    }

        //    if (sampleDescription.Contains("Elastics")) DryProcedure = "Tumble Dry";
        //    if (sampleDescription.Contains("Leathers")||sampleDescription.Contains("Swimwear")) DryProcedure = "Line Dry";
        //    if (sampleDescription.Contains("Cap")
        //            || sampleDescription.Contains("Gloves")
        //            || sampleDescription.Contains("Socks")) DryProcedure = dryProcedure;
        //    return DryProcedure;
        //}
        #endregion

        private string? DryProcedureHelper(
            List<FiberDto> fiberComposition,
            string sampleDescription,
            string? dryProcedure)
        {
            // 预计算只做一次
            var maxComp = _helper.MaxComposition(fiberComposition);
            var hasRegCell = _helper.IsCompositionDescExist("Regenerated Cellulose", fiberComposition);
            var over51Syn = _helper.IsCompositionSourceExist("Synthetic", fiberComposition) >= 51;

            // 1. 特殊小件直接透传
            if (sampleDescription.ContainsAny("Cap", "Gloves", "Socks")) return dryProcedure;

            // 2. 皮革、泳衣、松紧带 
            if (sampleDescription.ContainsAny("Leathers", "Swimwear")) return "Line Dry";
            if (sampleDescription.Contains("Elastics")) return "Tumble Dry";

            // 3. 成衣 / 毛衫
            if (sampleDescription.ContainsAny("Garment", "Knitwear"))
                return sampleDescription.Contains("Childrenswear") ? "Tumble Dry" : "Flat Dry";

            // 4. 针织物
            if (sampleDescription.ContainsAll("Knit", "Fabric"))
            {
                if (sampleDescription.Contains("Weft"))      // 纬编
                {
                    if (maxComp == "Cotton" || over51Syn) return "Tumble Dry";
                    if (maxComp is "Silk" or "Wool" or "Acrylic" || hasRegCell) return "Line Dry";
                }
                else if (sampleDescription.Contains("Warp")) // 经编
                {
                    return sampleDescription.Contains("Stretch") ? "Line Dry" : "Tumble Dry";
                }
                return null;                                 // 其他针织兜底
            }

            // 5. 机织物（Woven Fabric）
            if (sampleDescription.ContainsAll("Woven", "Fabric"))
            {
                // 5a. 里料 / 口袋
                if (sampleDescription.ContainsAny("Lining", "Pocket"))
                {
                    return maxComp is "Acetate" or "Silk" or "Viscose" or "Acrylic"
                        ? "Flat Dry"
                        : "Tumble Dry";
                }

                // 5b. 羊毛
                if (maxComp == "Wool")
                    return dryProcedure?.Contains("Tumble") == true ? "Tumble Dry" : "Line Dry";

                // 5c. 常规
                if (maxComp == "Cotton" || over51Syn || maxComp == "Silk") return "Tumble Dry";
                if (maxComp == "Linen") return "Flat Dry";
                if (hasRegCell) return "Line Dry";
            }

            return null; // 未命中任何规则
        }


        private string? WashingProcedureTranslationHelper(string WashingProcedure)
        {
            string washingProcedureTranslation = string.Empty;
            switch (WashingProcedure) 
            {
                case "4H":washingProcedureTranslation = "SHW";
                    break;
                case "3G":washingProcedureTranslation = "8A";
                    break;
                case "3M":washingProcedureTranslation = "Refer 6A";
                    break;
                case "3N":washingProcedureTranslation = "Refer 5A";
                    break;
                case "4G":washingProcedureTranslation = "7A";
                    break;
                case "4M":washingProcedureTranslation = "6A";
                    break;
                case "4N": washingProcedureTranslation = "5A";
                    break;
                case "5M":washingProcedureTranslation = "4A";
                    break;
                case "5N":washingProcedureTranslation = "Refer 5A";
                    break;
                case "6M":washingProcedureTranslation = "3A";
                    break;
                case "6N":washingProcedureTranslation = "2A";
                    break;
            }

            return washingProcedureTranslation;
        }
    }

    // 1. 文件级私有工具类
    file static class Ext
    {
        public static bool ContainsAny(this string s, params string[] keys)
            => keys.Any(k => s.Contains(k, StringComparison.OrdinalIgnoreCase));

        public static bool ContainsAll(this string s, params string[] keys)
            => keys.All(k => s.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

}
