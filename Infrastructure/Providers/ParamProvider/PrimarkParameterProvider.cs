using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.BuyerRepos;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using NX_lims_Softlines_Command_System.Domain.Model.Interface;


namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider
{
    public class PrimarkParameterProvider
    {
        private readonly FiberContentHelper _helper;
        private readonly PrimarkRepository _repo;

        public PrimarkParameterProvider(FiberContentHelper helper, PrimarkRepository repo)
        {
            _helper = helper;
            _repo = repo;
        }
        // 将硬编码的测试项目列表提取为静态只读常量
        public static readonly string[] WetTestItems = new[]
        {
            "Colour Fastness to Washing",
            "Absorbency of Textiles",
            "Colour Fastness to Hot Pressing",
            "Dimensional and Bra Wire Casing Stability",
            "Martindale Pilling",
            "Print / Motif / Flock Durability",
            "Print Durability",
            "Shower Resistant Claims Spray Rating",
            "Spirality", 
            "Stability to Dry Cleaning",
            "Stability to Washing",
            "Waterproof Claims Hydrostatic Head",
            "Dimensional Stability",
            "Security of Attachment(Wash)",
            "Easycare/Non-Iron",
            "Appearance-Common",
            "Colour Fastness to Dry Cleaning"
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

                var wetParams = CreateWetParameters(paramInput, fiberContent!, sample,sampleDesc);

                var existWetParam = await _repo.GetWetParamAsync(infoDto.reportNumber!, itemName, sample);

                if (existWetParam != null)await _repo.UpdateWetParamAsync(wetParams, existWetParam);
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
            var normalParameter = await CreatNormalParameters(sampleDesc,sample, itemName, infoDto, fiberContent);

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
            ("Colour Fastness to Washing", "4H" or "3M" or "3G" or "3H", _, "PTC03" or "PTC04" or "PTC24") => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Temperature = p.WashingProcedure!.Contains("3") == true ? "30" : "40",
                Program = p.WashingProcedure.Contains("3") == true ? "ref A2S" : "A2S",
                SteelBallNum = 0,
                SpecialCareInstruction = (FetchSamplePropertyValue(sampleDesc,"Color").Contains("White") || FetchSamplePropertyValue(sampleDesc, "Color").Contains("Cream")) == true ? "N/A" : null
            },
            ("Colour Fastness to Washing", "4N" or "4M" or "4G" or "3N", _, "PTC03" or "PTC04" or "PTC24") => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Temperature = p.WashingProcedure!.Contains("3") == true ? "30" : "40",
                Program = p.WashingProcedure.Contains("3") == true ? "ref A2S" : "A2S",
                SteelBallNum = _helper.IsCompositionExist("Animal", fiberContent!) == true ? 0 : 10,
                SpecialCareInstruction = (FetchSamplePropertyValue(sampleDesc, "Color").Contains("White") || FetchSamplePropertyValue(sampleDesc, "Color").Contains("Cream")) == true ? "N/A" : null
            },
            ("Colour Fastness to Washing", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Temperature = "40",
                Program = "A2S",
                SteelBallNum = _helper.IsCompositionExist("Animal", p.FiberContent!) == true ? 0 : 10,
                SpecialCareInstruction = (FetchSamplePropertyValue(sampleDesc, "Color").Contains("White") || FetchSamplePropertyValue(sampleDesc, "Color").Contains("Cream")) == true ? "N/A" : null
            },
            ("Absorbency of Textiles", "3H" or "4H", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("3") ? "80" : "105",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Absorbency of Textiles", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Program = WetParamHelper(p.WashingProcedure!),
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature =
                p.WashingProcedure!.Contains("3") ? "80"
                : p.WashingProcedure.Contains("4") ? "105"
                : p.WashingProcedure.Contains("5") ? "120"
                : "140",
                Bleach = p.WashingProcedure!.Contains("N") ? "Normal"
                : p.WashingProcedure.Contains("G") ? "Gentle"
                : p.WashingProcedure.Contains("M") ? "Permanent Press"
                : "",
                //Cycle，程度暂时用Bleach字段代替
                DryCleanProcedure = DryConditionHelper(p.DryProcedure!),
                //DryCondition，暂时用干洗字段代替
                AfterWash = "20",
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Wind Resistant Claims Air Permeability", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                SpecialCareInstruction = p.Sci,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                DryCleanProcedure = p.DCProcedure,
                AfterWash = "After 1 Wash",
            },
            ("Colour Fastness to Hot Pressing", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Temperature = HotPressingHelper(p.IronMethod, p.MenuName!),
                IronMethod = p.IronMethod ?? null,
            },
            ("Dimensional and Bra Wire Casing Stability", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = "4H",
                Temperature = "40",
                SpecialCareInstruction = p.Sci,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                DryProcedure = p.DryProcedure,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
            },
            ("Martindale Pilling", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                SpecialCareInstruction = p.Sci,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                DryProcedure = p.DryProcedure,
                AfterWash = "After 1 Wash",
            },
            ("Print / Motif / Flock Durability", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                Temperature = "40",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Detergent = "160 g ECE Detergent (with phosphate) (4g/L) and 40 g Sodium Perborate (1g/L)"
            },
            ("Print Durability", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                Temperature = "30",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Detergent = "160 g ECE Detergent (with phosphate) (4g/L) and 40 g Sodium Perborate (1g/L)"
            },
            ("Shower Resistant Claims Spray Rating", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                SpecialCareInstruction = p.Sci,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                DryProcedure = p.DryProcedure,
                AfterWash = "After 1 Wash",
            },
            ("Spirality", _, _, "PTC09" or "PTC10" or "PTC13" or "PTC14" or "PTC15" or "PTC15A" or "PTC29") => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                SpecialCareInstruction = p.Sci,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                DryProcedure = p.DryProcedure,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
            },
            ("Stability to Dry Cleaning", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", fiberContent!) == true ||
                      p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null
            },
            ("Colour Fastness to Dry Cleaning", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryCleanProcedure =p.DCProcedure,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", fiberContent!) == true ||
          p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N",
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null
            },
            ("Stability to Washing", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = "4N",
                Temperature = "40",
                SpecialCareInstruction = p.Sci,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                DryProcedure = p.DryProcedure,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
            },
            ("Waterproof Claims Hydrostatic Head", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = "4N",
                Temperature = "40",
                SpecialCareInstruction = p.Sci,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                DryProcedure = p.DryProcedure,
                AfterWash = "After 1 Wash",
            },
            ("Dimensional Stability", "3H" or "4H", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                SpecialCareInstruction = p.Sci,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                Temperature = "40",
                WashingProcedure = "4H",
                Detergent = null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                AfterWash = "5,23,32,45"
            },
            ("Dimensional Stability", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                SpecialCareInstruction = p.Sci,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                Temperature = p.WashingProcedure!.Contains("3") ? "30" : "40",
                WashingProcedure = p.WashingProcedure,
                Detergent = DetergentHelper(p.Detergent, sampleDesc, p.WashingProcedure),
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                AfterWash = "5,23,32,45"
            },
            ("Spirality", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                SpecialCareInstruction = p.Sci,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                Temperature = p.WashingProcedure!.Contains("3") ? "30" : "40",
                WashingProcedure = p.WashingProcedure,
                Detergent = DetergentHelper(p.Detergent, sampleDesc!, p.WashingProcedure),
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Security of Attachment(Wash)", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                DryProcedure = p.DryProcedure,
                SpecialCareInstruction = p.Sci,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", fiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", fiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                Temperature = p.WashingProcedure!.Contains("3") ? "30" : "40",
                WashingProcedure = p.WashingProcedure,
                Detergent = DetergentHelper(p.Detergent, sampleDesc!, p.WashingProcedure),
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("Easycare/Non-Iron", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
            },
            ("Appearance-Common", _, _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ContactSample = sample,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
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
            //List<SampleInfoDescription> sampleDesc= await _repo.GetSampleInfoDescription(sampleInfo, infoDto.reportNumber!,infoDto.buyer!);

            var param = string.Empty;
            var condition = string.Empty;

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
                case "Colour Fastness to Non Chlorine Bleach":
                    condition = BleachHelper(fiberContent!);
                    param = condition switch
                    {
                        string s when s.Contains("N/A") => @"{""IsApplicable"":""N/A""}",
                        string s when s.Contains("Else") => @"{""IsApplicable"":""Yes""}",
                        _ => @"{""IsApplicable"":""Yes""}"
                    };
                    break;
                case "Colour Fastness to Chlorine Bleach":
                    condition = BleachHelper(fiberContent!);
                    param = condition switch
                    {
                        string s when s.Contains("N/A") => @"{""IsApplicable"":""N/A""}",
                        string s when s.Contains("Else") => @"{""IsApplicable"":""Yes""}",
                        _ => @"{""IsApplicable"":""Yes""}"
                    };
                    break;
                case "Colour Fastness to Chlorinated Water":
                    condition = FetchSamplePropertyValue(sampleDesc, "Apparel Type");
                    param = condition switch
                    {
                        string s when s.Contains("Swimwear") => @"{""Concentration"":""50mg/L""}",
                        string s when s.Contains("Beachwear") => @"{""Concentration"":""20mg/L""}",
                        _ => @"{""Concentration"":""20mg/L""}"
                    };
                    break;
                case "Colour Fastness to Light":
                    condition = FetchSamplePropertyValue(sampleDesc, "Color");
                    param = condition switch
                    {
                        string s when s.Contains("Neon") && infoDto.menuName != "PTC01" => @"{""illumination "":""L-3""}",
                        _ => @"{""illumination "":""L-4""}"
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
                case "Martindale Pilling":
                    condition = PillingHelper(fiberContent!, FetchSamplePropertyValue(sampleDesc, "Fiber Type(Only for Pilling Resistance)"), infoDto.menuName!);
                    param = condition switch
                    {
                        string s when s.Contains("N/A") => @"{""IsApplicable"":""N/A""}",
                        string s when FetchSamplePropertyValue(sampleDesc, "Structure").Contains("Woven") || infoDto.menuName == "PTC01" => @"{""Cycle"":""2000 revs""}",
                        string s when infoDto.menuName == "PTC07" || infoDto.menuName == "PTC08" || infoDto.menuName == "PTC09" || infoDto.menuName == "PTC10" || infoDto.menuName == "PTC11" || infoDto.menuName == "PTC12" => @"{""Cycle"":""500 revs""}",
                        _ => @"{""Cycle"":""500 revs""}",
                    };
                    break;
                case "Residual Elongation":
                    condition = ElogationHelper(fiberContent!, sampleDesc, infoDto.menuName!);
                    param = condition switch
                    {
                        string s when s.Contains("N/A") => @"{""IsApplicable"":""N/A""}",
                        string s when s.Contains("15") => @"{""Load"":""15N""}",
                        string s when s.Contains("20") => @"{""Load"":""20N""}",
                        string s when s.Contains("25") => @"{""Load"":""25N""}",
                        string s when s.Contains("30") => @"{""Load"":""30N""}",
                        string s when s.Contains("40") => @"{""Load"":""140N""}",
                        _ => @"{""Load"":""140N""}"
                    };
                    break;
                case "Tear Strength":
                    bool isCelluloseExist = _helper.IsCompositionExist("Cellulose", fiberContent!);
                    if ( ! FetchSamplePropertyValue(sampleDesc,"Structure").Contains("Woven")||(_helper.CompositionRate(fiberContent!, "Elastane") == 0 && !isCelluloseExist)) condition = "N/A";
                    param = condition switch
                    {
                        string s when s.Contains("N/A") => @"{""IsApplicable"":""N/A""}",
                        _ => @"{""IsApplicable"":""Yes""}",
                    };
                    break;
                case "Tensile Strength":
                    if ( ! FetchSamplePropertyValue(sampleDesc, "Structure").Contains("Woven") || _helper.CompositionRate(infoDto.fiberComposition!, "Elastane") == 0) condition = "N/A";
                    param = condition switch
                    {
                        string s when s.Contains("N/A") => @"{""IsApplicable"":""N/A""}",
                        _ => @"{""IsApplicable"":""Yes""}",
                    };
                    break;
                case "Seam Strength":
                    if (!FetchSamplePropertyValue(sampleDesc, "Structure").Contains("Woven") || _helper.CompositionRate(infoDto.fiberComposition!, "Elastane") == 0) condition = "N/A";
                    param = condition switch
                    {
                        string s when s.Contains("N/A") => @"{""IsApplicable"":""N/A""}",
                        _ => @"{""IsApplicable"":""Yes""}",
                    };
                    break;
                case "Seam Slippage":
                    if (!FetchSamplePropertyValue(sampleDesc, "Structure").Contains("Woven") || _helper.CompositionRate(infoDto.fiberComposition!, "Elastane") == 0) condition = "N/A";
                    param = condition switch
                    {
                        string s when s.Contains("N/A") => @"{""IsApplicable"":""N/A""}",
                        _ => @"{""IsApplicable"":""Yes""}",
                    };
                    break;
                case "Unrecovered Elongation":
                    condition = FetchSamplePropertyValue(sampleDesc, "Apparel Type").Contains("Jeans")?"40":"30";
                    param = condition switch
                    {
                        string s when s.Contains("40") => @"{""Load"":""40N""}",
                        string s when s.Contains("30") => @"{""Load"":""30N""}",
                        _ => @"{""Load"":""30N""}"
                    };
                    break;
                case "Waterproof Claims Hydrostatic Head":
                    condition = FetchSamplePropertyValue(sampleDesc, "Test Method(for WaterProof)").Contains("WaterProof")?"1600": "1000";
                    param = condition switch
                    {
                        string s when s.Contains("1600") => @"{""Pressure"":""1600mmH2O""}",
                        string s when s.Contains("1000") => @"{""Pressure"":""1000mmH2O""}",
                        string s when infoDto.menuName!.Contains("PTC37") && FetchSamplePropertyValue(sampleDesc, "State").Contains("Fabric") => @"{""Pressure"":""10000mmH2O""}",
                        string s when infoDto.menuName!.Contains("PTC37") && FetchSamplePropertyValue(sampleDesc, "Test Method(for WaterProof)").Contains("Seam Proof") => @"{""Pressure"":""8000mmH2O""}",
                        _ => @"{""Pressure"":""10000mmH2O""}"
                    };
                    break;
                case "Abrasion of Knitted Footwear Garments - Modified Martindale": 
                    param = @"{""Load"":""12kPa"",""Cycle"":""8000 revs""}";
                    break;
                case "Quick Dry":
                    param = @"{""Remark"":""≤20 minutes or ≥ 0.6mL/h""}";
                    break;
                case "Bursting Strength":
                    param = @"{""Remark"":""Diameter: 79.8mm,Square:50cm²""}";
                    break;
                case "Colour Fastness to Dry Cleaning":
                    param = @"{""Remark"":""Multi-Fibre Type:SDC""}";
                    break;
                case "Colour Fastness to Washing":
                    param = @"{""Remark"":""Multi-Fibre Type:LyoW""}";
                    break;
                case "Nap Stability":
                    param = @"{""Cycle"":""4000 revs""}";
                    break;
                case "Residual Elongation SHAPEWEAR":
                    param = @"{""Load"":""36N""}";
                    break;
                case "Elastic Extension and Modulus Test":
                    param = @"{""Remark"":""Titan (CRE).    (Three cycles. Machine speed: 500mm/min)""}";
                    break;
                case "Vertical Wicking of Textiles":
                    param = @"{""Remark"":""Minimum 2.5 inches per 10 minutes""}";
                    break;
                case "Back Pocket Application Strength":
                    param = @"{""Remark"":""{Lightweight <100gms：150N;  Medium weight 101~199gms：175N; Heavyweight >200gms：200N }""}";
                    break;
                case "Belt Loop Application Strength":
                    param = @"{""Remark"":""{Lightweight <100gms：150N;  Medium weight 101~199gms：175N; Heavyweight >200gms：200N }""}";
                    break;
            }

            if(param==null||param=="") return new NormalParameter();

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
        private string? WetParamHelper(string WashingProcedure)
        {
            if (WashingProcedure == null) return null;
            string part_1 = "";
            string part_2 = "";
            part_1 =
            WashingProcedure!.Contains("N") ? "(1)"
            : WashingProcedure.Contains("G") ? "(2)"
            : WashingProcedure.Contains("P") ? "(3)"
            : "";
            part_2 =
                WashingProcedure!.Contains("3") ? "II"
                : WashingProcedure.Contains("4") ? "III"
                : WashingProcedure.Contains("5") ? "IV"
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

        private string? HotPressingHelper(string? IronMethod, string MenuName)
        {
            string? Temperature = null;
            Temperature = IronMethod!.Contains("Cool") ? "100"
                : IronMethod!.Contains("Warm") ? "150"
                : IronMethod!.Contains("Hot") ? "200"
                : "/";
            if ((MenuName == "PTC35" || MenuName == "PTC36") && Temperature == "100") Temperature = "110";
            return Temperature;
        }

        private string? PillingHelper(List<FiberDto> fiberComposition, string sampleDescription, string MenuName)
        {
            string? Result = null;
            //仅适用于合成纤维，羊毛，晴纶，及其混纺物
            //长丝不做
            //抓绒只测试正面
            if(sampleDescription.Contains("Filament"))return Result = "N/A";
            var rateS = _helper.CompositionRate(fiberComposition, "Silk");
            if (rateS > 0) return Result = "N/A";
            var rateW = _helper.CompositionRate(fiberComposition, "Wool");
            var rateQ = _helper.CompositionRate(fiberComposition, "Acrylic");
            if (rateW == 0 && rateQ == 0 && _helper.IsCompositionSourceExist("Synthetic", fiberComposition) == 0) return Result = "N/A";
            return Result;
        }

        private string? BleachHelper(List<FiberDto> fiberComposition)
        {
            string? Result = "Else";
            var rateE = _helper.CompositionRate(fiberComposition, "Elastane");
            var rateW = _helper.CompositionRate(fiberComposition, "Wool");
            var rateS = _helper.CompositionRate(fiberComposition, "Silk");
            if (rateE > 0 || rateW > 0 || rateS > 0) return Result = "N/A";
            else return Result;
        }

        private string? ElogationHelper(List<FiberDto> fiberComposition, List<SampleInfoDescription> sampleDesc, string MenuName)
        {
            string? Result = "N/A";
            var rate = _helper.CompositionRate(fiberComposition, "Elastane");
            if (rate == 0) return Result = "N/A";
            if (MenuName == "PTC07" || MenuName == "PTC08") return Result = "20";
            else if (MenuName == "PTC01" || MenuName == "PTC02" || MenuName == "PTC04")
            {
                if (FetchSamplePropertyValue(sampleDesc, "Apparel Type").Contains("Jeans")) return Result = "40";
                else return Result = "30";
            }
            else if (MenuName == "PTC18A" || MenuName == "PTC18B" || MenuName == "PTC19")
            {
                if (!FetchSamplePropertyValue(sampleDesc, "Apparel Type").Contains("Jersey")) return Result = "15";
                if (rate.HasValue && rate < 5) return Result = "15";
                if (rate.HasValue && 5 <= rate && rate < 11) return Result = "20";
                if (rate.HasValue && rate >= 11) return Result = "25";
            }
            else if (MenuName == "PTC14" || MenuName == "PTC13")
            {
                if (FetchSamplePropertyValue(sampleDesc, "Structure").Contains("Woven")) return Result = "30";
                if (rate.HasValue && rate < 5) return Result = "15";
                if (rate.HasValue && 5 <= rate && rate < 11) return Result = "20";
                if (rate.HasValue && rate >= 11) return Result = "25";
            }
            else
            {
                if (FetchSamplePropertyValue(sampleDesc, "Structure").Contains("Woven")) return Result = "30";
                if (FetchSamplePropertyValue(sampleDesc, "Structure").Contains("Knit")) return Result = "20";
            }
            return Result;
        }

        private string? DetergentHelper(string? detergent, List<SampleInfoDescription> sampleDesc, string WashingProcedure)
        {
            if (string.IsNullOrEmpty(detergent) == false && detergent == "Mild Detergent") return "20g Mild Detergent";
            if ((FetchSamplePropertyValue(sampleDesc, "Color").Contains("White") || FetchSamplePropertyValue(sampleDesc, "Color").Contains("Cream"))) return "20g 77%IEC(A) + 3%TAED + 20%Sodium Perborate";
            if (WashingProcedure.Contains("H")) return "60mL PERWOLL liquid for hand wash(4H)";
            return "20g 77%ECE(A)+ 3%TAED + 20%Sodium Perborate";
        }

        private string? AfterWashingHelper(string? AfterWashing)
        {
            var Result = AfterWashing?.Any() == true ? string.Join(",", AfterWashing) : null;
            return Result;
        }

        /*----------------------------------------------------------------------------------------------------------------------------*/












        #region
        //public async Task<string?> CreateParameters([FromBody] RequiredInfoDto infoDto, string ItemName)
        //{

        //    // 1. 计算最大值
        //    string? largestVarName = await _helper.MaxCompositionType(infoDto.fiberComposition!)!;
        //    string? Condition = null;
        //    string? Condition1 = null;
        //    switch (ItemName)
        //    {
        //        case "Accelerotor":
        //            if (infoDto.sampleDescription!.Contains("Velvet")) Condition = "3min";
        //            else if (infoDto.sampleDescription!.Contains("Corduroy") || infoDto.sampleDescription.Contains("Velour")) Condition = "5min";
        //            else Condition = null;
        //            break;
        //        case "Colour Fastness to Non Chlorine Bleach":
        //            Condition = BleachHelper(infoDto.fiberComposition!);
        //            break;
        //        case "Colour Fastness to Chlorine Bleach":
        //            Condition = BleachHelper(infoDto.fiberComposition!);
        //            break;
        //        case "Colour Fastness to Chlorinated Water":
        //            if (infoDto.sampleDescription!.Contains("Swimwear")) Condition = "50";
        //            else if (infoDto.sampleDescription!.Contains("Beachwear")) Condition = "20";
        //            else Condition = "20";
        //            break;
        //        case "Colour Fastness to Light":
        //            Condition = "L-4";
        //            if (infoDto.sampleDescription!.Contains("Neon") && infoDto.menuName != "PTC01") Condition = "L-3";
        //            break;
        //        case "Colour Fastness to Water":
        //            if (infoDto.sampleDescription!.Contains("White") || infoDto.sampleDescription.Contains("Cream")) Condition = "N/A";
        //            else Condition = null;
        //            break;
        //        case "Martindale Abrasion":
        //            if (infoDto.menuName == "PTC03" || infoDto.menuName == "PTC04" || infoDto.menuName == "PTC37") Condition = "no change shade";
        //            else Condition = null;
        //            break;
        //        case "Martindale Pilling":
        //            Condition1 = PillingHelper(infoDto.fiberComposition!, infoDto.sampleDescription!, infoDto.menuName!);
        //            if (Condition1 != "N/A")
        //            {
        //                if (infoDto.menuName == "PTC01") Condition = "2000";
        //                else if (infoDto.menuName == "PTC07" || infoDto.menuName == "PTC08" || infoDto.menuName == "PTC09" || infoDto.menuName == "PTC10" || infoDto.menuName == "PTC11" || infoDto.menuName == "PTC12")
        //                    Condition = "500";
        //                else
        //                {
        //                    if (infoDto.sampleDescription!.Contains("Woven")) Condition = "2000";
        //                    else if (infoDto.sampleDescription!.Contains("Knit")) Condition = "500";
        //                    else Condition = null;
        //                };
        //            }
        //            break;
        //        case "Residual Elongation":
        //            //Condition = ElogationHelper(infoDto.fiberComposition!, infoDto.sampleDescription!, infoDto.menuName!);
        //            break;
        //        case "Tear Strength":
        //            if (!infoDto.sampleDescription!.Contains("Woven")) Condition = "N/A";
        //            bool isCelluloseExist = _helper.IsCompositionExist("Cellulose", infoDto.fiberComposition!);
        //            if (_helper.CompositionRate(infoDto.fiberComposition!, "Elastane") ==0&& !isCelluloseExist) Condition = "N/A";
        //            break;
        //        case "Tensile Strength":
        //            if (!(infoDto.sampleDescription!.Contains("Woven") || _helper.CompositionRate(infoDto.fiberComposition!, "Elastane") == 0)) Condition = "N/A";
        //            break;
        //        case "Seam Strength":
        //            if (!(infoDto.sampleDescription!.Contains("Woven") || _helper.CompositionRate(infoDto.fiberComposition!, "Elastane") == 0)) Condition = "N/A";
        //            break;
        //        case "Seam Slippage":
        //            if (!(infoDto.sampleDescription!.Contains("Woven") || _helper.CompositionRate(infoDto.fiberComposition!, "Elastane") == 0)) Condition = "N/A";
        //            break;
        //        case "Unrecovered Elongation":
        //            if (infoDto.sampleDescription!.Contains("Jeans")) Condition = "40";
        //            else Condition = "30";
        //            break;
        //        case "Waterproof Claims Hydrostatic Head":
        //            if (infoDto.sampleDescription!.Contains("WaterProof"))
        //            {
        //                Condition = "1600";
        //            }
        //            else if (infoDto.sampleDescription!.Contains("Repellent"))
        //            {
        //                Condition = "1000";
        //            }
        //            if (infoDto.menuName!.Contains("PTC37"))
        //            {
        //                if (infoDto.sampleDescription!.Contains("Fabric"))
        //                {
        //                    Condition = "10000";
        //                }
        //                else if (infoDto.sampleDescription!.Contains("Seam"))
        //                {
        //                    Condition = "8000";
        //                }
        //            }
        //            break;
        //    }

        //    return GetParameter(Condition, ItemName, Condition1);//返回一个string类型的Parameter
        //}

        // ---------- 2. 映射表 ----------
        //private static readonly Dictionary<(string? Condition, string Item, string? Lv), string?> _map = new()
        //{
        //    //[(null, "Abrasion of Knitted Footwear Garments - Modified Martindale", null)] = "Load:12kPa, Cycle: 8000 revs",
        //    //[("3min", "Accelerotor", null)] = "Time:3min,Cycle: 2000R.P.M",
        //    //[("5min", "Accelerotor", null)] = "Time:5min,Cycle: 2000R.P.M",
        //    //[(null, "Accelerotor", null)] = "Time:5min,Cycle: 2000R.P.M",
        //    //[(null, "Quick Dry", null)] = "≤20 minutes or ≥ 0.6mL/h",
        //    //[(null, "Bursting Strength", null)] = "Diameter: 79.8mm,Square:50cm²",
        //    //[("20", "Colour Fastness to Chlorinated Water", null)] = "20mg/L",
        //    //[("50", "Colour Fastness to Chlorinated Water", null)] = "50mg/L",
        //    //[("N/A", "Colour Fastness to Non Chlorine Bleach", null)] = "N/A",
        //    //[("N/A", "Colour Fastness to Chlorine Bleach", null)] = "N/A",
        //    //[("Else", "Colour Fastness to Non Chlorine Bleach", null)] = "-",
        //    //[("Else", "Colour Fastness to Chlorine Bleach", null)] = "-",
        //    //[(null, "Colour Fastness to Dry Cleaning", null)] = "Multi-Fibre Type:SDC",
        //    //[("L-3", "Colour Fastness to Light", null)] = "L-3",
        //    //[("L-4", "Colour Fastness to Light", null)] = "L-4",
        //    //[("N/A", "Colour Fastness to Water", null)] = "N/A",
        //    //[(null, "Colour Fastness to Washing", null)] = "Multi-Fibre Type:LyoW",
        //    //[(null, "Colour Fastness to Water", null)] = "Multi-Fibre Type:LyoW",
        //    //[(null, "Martindale Abrasion", null)] = "9KPa,Shade Change @ 5000 {<100g/m²：10000 rubs；101~199g/m²：15000 rubs；>2000g/m²：20000 rubs}",
        //    //[("no change shade", "Martindale Abrasion", null)] = "9KPa；{<200g/m²：10000 rubs；201~270g/m²：15000 rubs；271~390g/m²：18000 rubs；>390g/m²：20000 rubs}",
        //    //[("2000", "Martindale Pilling", null)] = "Cycle:2000 revs",
        //    //[("500", "Martindale Pilling", null)] = "Cycle:500 revs",
        //    //[(null, "Martindale Pilling", "N/A")] = "N/A",
        //    //[(null, "Nap Stability", null)] = "Cycle:4000 revs",
        //    //[("N/A", "Residual Elongation", null)] = "N/A",
        //    //[("15", "Residual Elongation", null)] = "Load:15N",
        //    //[("20", "Residual Elongation", null)] = "Load:20N",
        //    //[("25", "Residual Elongation", null)] = "Load:25N",
        //    //[("30", "Residual Elongation", null)] = "Load:30N",
        //    //[("40", "Residual Elongation", null)] = "Load:40N",
        //    //[(null, "Residual Elongation SHAPEWEAR", null)] = "Load:36N",
        //    //[("N/A", "Tear Strength", null)] = "N/A",
        //    //[("N/A", "Tensile Strength", null)] = "N/A",
        //    //[("N/A", "Seam Strength", null)] = "N/A",
        //    //[("N/A", "Seam Slippage", null)] = "N/A",
        //    //[("40", "Unrecovered Elongation", null)] = "Load:40N",
        //    //[("30", "Unrecovered Elongation", null)] = "Load:30N",
        //    //[(null, "Elastic Extension and Modulus Test", null)] = "Titan (CRE).    (Three cycles. Machine speed: 500mm/min)",
        //    //[(null, "Vertical Wicking of Textiles", null)] = "Minimum 2.5 inches per 10 minutes",
        //    //[(null, "Back Pocket Application Strength", null)] = "{Lightweight <100gms：150N;  Medium weight 101~199gms：175N; Heavyweight >200gms：200N }",
        //    //[(null, "Belt Loop Application Strength", null)] = "{Lightweight <100gms：150N;  Medium weight 101~199gms：175N; Heavyweight >200gms：200N }",
        //    //[("1600", "Waterproof Claims Hydrostatic Head", null)] = "1600mmH2O",
        //    //[("1000", "Waterproof Claims Hydrostatic Head", null)] = "1000mmH2O",
        //    //[("8000", "Waterproof Claims Hydrostatic Head", null)] = "8000mmH2O",
        //    //[("10000", "Waterproof Claims Hydrostatic Head", null)] = "10000mmH2O"
        //};

        //private static string? GetParameter(string? Condition, string item, string? lv)
        //{
        //    // 1) 先精确匹配 (Menu, Item, Lv)
        //    if (_map.TryGetValue((Condition, item, lv), out var exact)) return exact;

        //    // 2) 再匹配 (Menu, Item, any)
        //    if (_map.TryGetValue((Condition, item, null), out var fallback)) return fallback;

        //    return null!;
        //}
        #endregion

    }
}