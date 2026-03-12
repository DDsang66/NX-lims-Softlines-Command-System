using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.BuyerRepos;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider
{
    public class OvsParameterProvider
    {
        private readonly FiberContentHelper _helper;
        private readonly OvsRepository _repo;

        public OvsParameterProvider(FiberContentHelper helper, OvsRepository repo)
        {
            _helper = helper;
            _repo = repo;
        }
        // 将硬编码的测试项目列表提取为静态只读常量
        public static readonly string[] WetTestItems = new[]
        {
            "Colour Fastness to Washing",
            "Dimensional Stability to Washing",
            "Dimensional Stability to Dry-Cleaning",
            "Accelerated Ageing(Stroage) Test",
            "Moisture Management",
            "Pilling Resistance",
            "Bursting Strength",
            "Seam Slippage",
            "Vertical Wicking",
            "Water Permeability/Hydrostatic Head",
            "Spray Test",
            "Air Permeability",
            "Absorbency",
        };

        // 为了提高查找效率，同时提供一个 HashSet 版本
        public static readonly HashSet<string> WetTestItemsSet = new HashSet<string>(WetTestItems);

        /// <summary>
        /// 根据ItemName生成对应的参数
        /// </summary>
        /// <param name="infoDto"></param>
        /// <param name="ItemName"></param>
        public async Task<(WetParameterIso wetParameter, NormalParameter normalParam)> CreateParamGeneratorAsync(
            [FromBody] RequiredInfoDto infoDto,
            string itemName,
            string standard,
            string sample)
        {
            var wetParameter = new WetParameterIso();

            var normalParam = new NormalParameter();

            if (string.IsNullOrWhiteSpace(itemName)) return (wetParameter, normalParam);

            //获取SampleInfo(根据测点Code获取对应的测点信息)
            var sampleInfo = await _repo.GetSampleByNameAsync(sample, infoDto.reportNumber!, infoDto.buyer!);

            List<SampleInfoDescription> sampleDesc = await _repo.GetSampleInfoDescription(sampleInfo, infoDto.reportNumber!, infoDto.buyer!);

            //获取Composition(根据测点Code获取对应的成分信息)
            var fiberContent = infoDto.fiberCompositionSingle!.Where(x => x.Sample == sample).FirstOrDefault()!.Composition;

            /*生成缩水参数----------------------------------------------------------------------------------------------------------*/
            if (WetTestItemsSet.Contains(itemName))
            {
                var paramInput = new ParamsInput().CreateParamsInput(infoDto, itemName, standard);

                var wetParams = CreateWetParameters(paramInput, fiberContent!, sample, sampleDesc);

                var existWetParam = await _repo.GetWetParamAsync(infoDto.reportNumber!, itemName, sample);

                if (existWetParam != null) await _repo.UpdateWetParamAsync(wetParams, existWetParam);
                else
                {
                    //如果不存在，创建后更新
                    var newWetParam = await _repo.CreateWetParamAsync(paramInput);
                    await _repo.UpdateWetParamAsync(wetParams, newWetParam);
                }

                wetParameter = wetParams;//返回生成的缩水参数
            }
            /*----------------------------------------------------------------------------------------------------------------------------*/

            //通用参数生成逻辑,调用规则字典把相关的测点信息、测试条件、测试方法传入，最后输出参数
            var normalParameter = await CreatNormalParameters(sampleDesc, sample, itemName, infoDto, fiberContent);

            normalParam = normalParameter;

            return (wetParameter, normalParam);
        }

        /// <summary>
        /// 根据ItemName生成对应的水洗参数
        /// </summary>
        /// <param name="p"></param>
        /// <param name="sample"></param>
        /// <param name="fiberContent"></param>
        /// <returns></returns>
        private WetParameterIso CreateWetParameters(
            ParamsInput p,
            List<FiberDto>? fiberContent,
            string sample,
            List<SampleInfoDescription> sampleDesc) => (p.ItemName, p.WashingProcedure, p.DCProcedure, p.MenuName) switch
            {
                ("Colour Fastness to Washing", _,_,_) => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
                    Standard = p.Standard,
                    ReportNumber = p.OrderNumber!,
                    Temperature = WashingProcedureBuilder(p.WashingProcedure, p.MenuName!, fiberContent!).Contains("6") == true ? "60" : "40",
                    Program = WashingProcedureBuilder(p.WashingProcedure, p.MenuName!, fiberContent!).Contains("6") == true ? "C2S" : "A2S",
                    SteelBallNum = BallNumberBuilder(p.MenuName!, fiberContent!),
                    SteelBallType = "Steel Ball",
                },
                ("Dimensional Stability to Washing", _, _,_) => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
                    Standard = p.Standard,
                    ReportNumber = p.OrderNumber!,
                    WashingProcedure = WashingProcedureBuilder(p.WashingProcedure, p.MenuName!, fiberContent!),
                    Temperature = WashingProcedureBuilder(p.WashingProcedure, p.MenuName!, fiberContent!).Contains("6") ? "60"
                    : WashingProcedureBuilder(p.WashingProcedure, p.MenuName!, fiberContent!).Contains("3") ? "30" : "40",
                    DryProcedure = DryProcedureBuilder(p.DryProcedure, p.MenuName!, fiberContent!),
                    Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                    : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                    : "Type III (100% Polyester)",
                    SpecialCareInstruction = p.Sci ?? null,
                    AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                    Iron = p.Iron ?? null,
                    IronMethod = p.IronMethod ?? null,
                },
                ("Dimensional Stability to Dry-Cleaning", _, _,_) => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
                    Standard = p.Standard,
                    ReportNumber = p.OrderNumber!,
                    Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", fiberContent!) == true ||
                                      p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
                    AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null
                },
                ("Accelerated Ageing(Stroage) Test", _, _, _) => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
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
                ("Moisture Management", _, _, _) => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
                    Standard = p.Standard,
                    ReportNumber = p.OrderNumber!,
                    WashingProcedure = "3N",
                    Temperature = "30",
                    DryProcedure = "Line Dry",
                    Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                    : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                    : "Type III (100% Polyester)",
                    SpecialCareInstruction = p.Sci ?? null,
                    AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                    Iron = p.Iron ?? null,
                    IronMethod = p.IronMethod ?? null,
                },
                ("Pilling Resistance", _, _, _) => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
                    Standard = p.Standard,
                    ReportNumber = p.OrderNumber!,
                    WashingProcedure = PillingWashingProcedureBuilder(p.WashingProcedure, fiberContent!),
                    Temperature = p.WashingProcedure!.Contains("4") ? "40" : "60",
                    DryProcedure = _helper.IsCompositionSourceExist("Animal", fiberContent!) > 0 ? "Flat Dry" : p.DryProcedure,
                    Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                    : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                    : "Type III (100% Polyester)",
                    SpecialCareInstruction = p.Sci ?? null,
                    AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                    Iron = p.Iron ?? null,
                    IronMethod = p.IronMethod ?? null,
                },
                ("Bursting Strength", _, _, _) => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
                    Standard = p.Standard,
                    ReportNumber = p.OrderNumber!,
                    WashingProcedure = _helper.CompositionRate(fiberContent!, "Silk") > 0 ? PillingWashingProcedureBuilder(p.WashingProcedure, fiberContent!) : p.WashingProcedure,
                    Temperature = p.WashingProcedure!.Contains("4") ? "40" : p.WashingProcedure!.Contains("6") ? "60" : "30",
                    DryProcedure = _helper.IsCompositionSourceExist("Animal", fiberContent!) > 0 ? "Flat Dry" : p.DryProcedure,
                    Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                    : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                    : "Type III (100% Polyester)",
                    SpecialCareInstruction = p.Sci ?? null,
                    AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                    Iron = p.Iron ?? null,
                    IronMethod = p.IronMethod ?? null,
                },
                ("Seam Slippage", _, _, _) => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
                    Standard = p.Standard,
                    ReportNumber = p.OrderNumber!,
                    WashingProcedure = _helper.CompositionRate(fiberContent!, "Silk") > 0 ? PillingWashingProcedureBuilder(p.WashingProcedure, fiberContent!) : p.WashingProcedure,
                    Temperature = p.WashingProcedure!.Contains("4") ? "40" : p.WashingProcedure!.Contains("6") ? "60" : "30",
                    DryProcedure = _helper.IsCompositionSourceExist("Animal", fiberContent!) > 0 ? "Flat Dry" : p.DryProcedure,
                    Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                    : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                    : "Type III (100% Polyester)",
                    SpecialCareInstruction = p.Sci ?? null,
                    AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                    Iron = p.Iron ?? null,
                    IronMethod = p.IronMethod ?? null,
                },
                ("Vertical Wicking", _, _, _) => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
                    Standard = p.Standard,
                    ReportNumber = p.OrderNumber!,
                    WashingProcedure = "3N",
                    Temperature = "30",
                    DryProcedure = "Line Dry",
                    Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                    : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                    : "Type III (100% Polyester)",
                    SpecialCareInstruction = p.Sci ?? null,
                    AfterWash = "3 Cycles",
                    Iron = p.Iron ?? null,
                    IronMethod = p.IronMethod ?? null,
                },
                ("Water Permeability/Hydrostatic Head", _, _, _) => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
                    Standard = p.Standard,
                    ReportNumber = p.OrderNumber!,
                    WashingProcedure = p.MenuName == "PP-Period Panties" ? "3N" : p.MenuName == "A-SKI wear" ? "4N" : p.WashingProcedure,
                    Temperature = p.MenuName == "PP-Period Panties" ? "30" : p.MenuName == "A-SKI wear" ? "30" : p.WashingProcedure!.Contains("3") ? "30" : "40",
                    DryProcedure = p.MenuName == "PP-Period Panties" ? "Line Dry" : p.MenuName == "A-SKI wear" ? "Line Dry" : p.DryProcedure,
                    Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                    : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                    : "Type III (100% Polyester)",
                    SpecialCareInstruction = p.Sci ?? null,
                    Iron = p.Iron ?? null,
                    IronMethod = p.IronMethod ?? null,
                },
                ("Spray Test", _, _, _) => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
                    Standard = p.Standard,
                    ReportNumber = p.OrderNumber!,
                    WashingProcedure = "4N",
                    Temperature = "40",
                    DryProcedure = p.DryProcedure,
                    Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
        : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
        : "Type III (100% Polyester)",
                    DryCleanProcedure = p.DCProcedure,
                    Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", fiberContent!) == true ||
                                      p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
                    SpecialCareInstruction = p.Sci ?? null,
                    Iron = p.Iron ?? null,
                    IronMethod = p.IronMethod ?? null,
                },
                ("Air Permeability", _, _, _) => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
                    Standard = p.Standard,
                    ReportNumber = p.OrderNumber!,
                    WashingProcedure = p.WashingProcedure,
                    Temperature = p.WashingProcedure!.Contains("6") ? "60"
                    : p.WashingProcedure.Contains("3") ? "30"
                    : "40",
                    DryProcedure = p.DryProcedure,
                    Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                    : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                    : "Type III (100% Polyester)",
                    SpecialCareInstruction = p.Sci ?? null,
                    Iron = p.Iron ?? null,
                    IronMethod = p.IronMethod ?? null,
                    AfterWash = p.MenuName!.Contains("I-SKI wear") ? "3 Cycles" : "5 Cycles",
                },
                ("Absorbency", _, _, _) => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
                    Standard = p.Standard,
                    ReportNumber = p.OrderNumber!,
                    WashingProcedure = "6N",
                    Temperature = "60",
                    DryProcedure = (p.MenuName == "O" || p.MenuName == "T") ? p.DryProcedure : "Tumble Dry",
                    Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                    : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                    : "Type III (100% Polyester)",
                    SpecialCareInstruction = p.Sci ?? null,
                    Iron = p.Iron ?? null,
                    IronMethod = p.IronMethod ?? null,
                    AfterWash = "1 Cycle",
                },
                _ => new WetParameterIso
                {
                    ContactItem = p.ItemName,
                    ContactSample = sample,
                    Standard = p.Standard,
                    ReportNumber = p.OrderNumber!
                }
            };

        /// <summary>
        /// 用于生成PHYParameter
        /// </summary>
        /// <param name="sampleInfo"></param>
        /// <param name="itemName"></param>
        /// <param name="infoDto"></param>
        /// <returns></returns>
        private async Task<NormalParameter> CreatNormalParameters(
            List<SampleInfoDescription> sampleDesc,
            string sample,
            string itemName,
            RequiredInfoDto infoDto,
            List<FiberDto>? fiberContent)
        {
            var param = string.Empty;
            var condition = string.Empty;
            var condition1 = string.Empty;

            //规则处理器
            switch (itemName)
            {
                case "Accelerotor":
                    condition = FetchSamplePropertyValue(sampleDesc, "Surface Morphology(Only for Accelerotor)");
                    param = condition switch
                    {
                        string s when s.Contains("Velvet") => @"{""Time"":""3min"",""Cycle"":""2000R.P.M""}",
                        string s when s.Contains("Corduroy") || s.Contains("Velour") => @"{""Time"":""5min"",""Cycle"":""2000R.P.M""}",
                        _ => @"{""Time"":""5min"",""Cycle"":""2000R.P.M""}"
                    };
                    break;
                case "Colour Fastness to Water":
                    condition = FetchSamplePropertyValue(sampleDesc, "Color");
                    param = condition switch
                    {
                        string s when s.Contains("White") && s.Contains("Cream") => @"{""IsApplicable"":""N/A"",""Cross"":""Cross Staning"",""Remark"":""Multi-Fibre Type:LyoW""}",
                        _ => @"{""IsApplicable"":""Yes"",""Cross"":""Cross Staning"",""Remark"":""Multi-Fibre Type:LyoW""}"
                    };
                    break;
                case "Martindale Abrasion":
                    if (infoDto.menuName == "PTC03" || infoDto.menuName == "PTC04" || infoDto.menuName == "PTC37") condition = "no change shade";
                    param = condition switch
                    {
                        string s when s.Contains("no change shade") => @"{""Load"":""9KPa"",""UnitWeight"":""{< 200g / m²：10000 rubs；201~270g / m²：15000 rubs；271~390g / m²：18000 rubs；> 390g / m²：20000 rubs}""}",
                        _ => @"{""Load"":""9KPa"",""ShadeChange"":""@ 5000 revs"",""UnitWeight"":""{<100g/m²：10000 rubs；101~199g/m²：15000 rubs；>200g/m²：20000 rubs}""}",
                    };
                    break;





                case "Colour Fastness to Rubbing on Leather":
                    condition = infoDto.menuName;
                    param = condition switch
                    {
                        "E" => @"{""TestMethod"":""Dry-50 Cycles; Wet-20 Cycles; Sweat-50 Cycles;""}",
                        "LG" => @"{""TestMethod"":""Dry-150 Cycles; Wet-50 Cycles;""}",
                        "KL&KP" => @"{""TestMethod"":""Dry-50 Cycles; Wet-50 Cycles;""}",
                        _ => @"{""TestMethod"":""Dry-50 Cycles; Wet-50 Cycles;""}"
                    };
                    break;
                case "Colour Fastness to Light":
                    var color = FetchSamplePropertyValue(sampleDesc, "Color");
                    // 获取 Product Type 字段
                    var productType = FetchSamplePropertyValue(sampleDesc, "Product Type");

                    // 特殊颜色类型映射
                    var specialColorTypes = new[] { "Turquoise", "Brilliant Color", "Fluo" };
                    var isSpecialColor = specialColorTypes.Any(c => color?.Contains(c) == true);

                    // 特殊产品类型映射
                    var specialProductTypes = new[] { "Lining", "Sweatband" };
                    var isSpecialProductType = specialProductTypes.Any(p => productType?.Contains(p) == true);

                    // 基础等级判断（根据 Product Type 判断基础等级）
                    var baseGrade = productType switch
                    {
                        string p when p.Contains("L") || p.Contains("L-SKI") || p.Contains("L-Act") ||
                                      p.Contains("PP") || p.Contains("P") => "L-3",
                        string p when p.Contains("N") || p.Contains("O") || p.Contains("T") ||
                                      p.Contains("U") || p.Contains("V") || p.Contains("Z") => "L-5",
                        string p when p.Contains("HTL-N-Bed Sheet") || p.Contains("HTL-T-Bathrobe&Towel") ||
                                      p.Contains("HTL-P-TableClothes") || p.Contains("HTL-S-SPA&Sea Towel") ||
                                      p.Contains("UPT-T") => "L-5",
                        _ => "L-4"
                    };

                    // 最终等级：如果是 L-5 且属于特殊颜色或特殊产品类型，则降级为 L-3
                    var finalGrade = (baseGrade == "L-5" && (isSpecialColor || isSpecialProductType)) ? "L-3" : baseGrade;
                    param = @"{""illumination "":"" " + finalGrade + @"""}";
                    break;
                case "Colour Fastness to Chlorinated Water":
                    condition = FetchSamplePropertyValue(sampleDesc, "Apparel Type");
                    param = condition switch
                    {
                        string s when s.Contains("Swimwear") || infoDto.menuName!.Contains("LG") => @"{""Concentration"":""50mg/L""}",
                        _ => @"{""Concentration"":""20mg/L""}"
                    };
                    break;
            }

            if (param == null || param == "") return new NormalParameter();

            //param存入数据库，暂时统一存入extraParam字段，后续可根据需要调整
            var existNormalParam = await _repo.GetNormalParamAsync(infoDto.reportNumber!, itemName, sample);
            if (existNormalParam != null)
            {
                await _repo.UpdateNormalParamAsync(param, existNormalParam);
                return existNormalParam;
            }
            else
            {
                var newParam = await _repo.CreateNormalParamAsync(infoDto.reportNumber!, itemName, sample);
                await _repo.UpdateNormalParamAsync(param, newParam);
                return newParam;
            }
        }

        /// <summary>
        /// 用于获取sampleDesc的指定属性的值
        /// </summary>
        /// <param name="sampleDesc"></param>
        /// <param name="indexPropertyName"></param>
        /// <returns></returns>
        private string FetchSamplePropertyValue(List<SampleInfoDescription> sampleDesc, string indexPropertyName)
        {
            var propertyValue = string.Empty;
            sampleDesc.ForEach(desc =>
            {
                if (desc.PropertyName == indexPropertyName)
                {
                    propertyValue = desc.PropertyValue;
                }
            });
            return propertyValue;
        }

        /*私有辅助,基础逻辑下沉--------------------------------------------------------------------------------------------*/
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
            else if (aniRate > 0 && WashingProcedure!.Contains("4")) return "4H";
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
                var ballNum = _helper.IsCompositionExist("Animal", fiberComposition!) == true ? 0 : 10;
                return ballNum;
            }
        }

        private string DryProcedureBuilder(string? DryProcedure, string Menuname, List<FiberDto> fiberComposition)
        {
            if (_helper.IsCompositionSourceExist("Animal", fiberComposition) > 0) return "Flat Dry";
            else return "Tumble Dry";
        }

        /*----------------------------------------------------------------------------------------------------------------------------*/

    }
}
