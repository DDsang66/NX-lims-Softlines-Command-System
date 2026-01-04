using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using DocumentFormat.OpenXml.Math;


namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider
{
    public class WoolworthParameterProvider
    {
        private readonly FiberContentHelper _helper;

        public WoolworthParameterProvider(FiberContentHelper helper)
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
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure!.Contains("N") ? "Cotton procedure"
                : p.WashingProcedure!.Contains("M") ? "Minimum iron procedure"
                : p.WashingProcedure!.Contains("G") ? "Delicates procedure"
                : "Wollens procedure",
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Detergent = "77％ECE(A)+3%TAED+20% sodium perborate",
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Program = "900r",
                AfterWash = "After 1 Wash"
            },
            ("Appearance", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure!.Contains("N") ? "Cotton procedure"
                : p.WashingProcedure!.Contains("M") ? "Minimum iron procedure"
                : p.WashingProcedure!.Contains("G") ? "Delicates procedure"
                : "Wollens procedure",
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Detergent = "77％ECE(A)+3%TAED+20% sodium perborate",
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Program = "900r",
                AfterWash = "After 1 Wash"
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
                case "Pilling Resistance":
                    if(standard.Contains("12945-1")) condition = "ICI";
                    else if (standard.Contains("12945-2")) condition = "Martindale";

                    if(_helper.CompositionRate(infoDto.fiberComposition!,"Wool")>0) condition1 = "Wool";
                    else if (infoDto.sampleDescription!.Contains("Woven")) condition1 = "Woven";
                    else if (infoDto.sampleDescription.Contains("Knit")) condition1 = "Knit";
                    break;
                case "CF to Light":
                    if (standard.Contains("12945-1")) condition = "ICI";
                    else if (standard.Contains("12945-2")) condition = "Martindale";

                    if (_helper.CompositionRate(infoDto.fiberComposition!, "Wool") > 0) condition1 = "Wool";
                    else if (infoDto.sampleDescription!.Contains("Woven")) condition1 = "Woven";
                    else if (infoDto.sampleDescription.Contains("Knit")) condition1 = "Knit";
                    break;
                case "Water Resistance-Hydrostatic Pressure":
                    if (standard.Contains("12945-1")) condition = "ICI";
                    else if (standard.Contains("12945-2")) condition = "Martindale";

                    if (_helper.CompositionRate(infoDto.fiberComposition!, "Wool") > 0) condition1 = "Wool";
                    else if (infoDto.sampleDescription!.Contains("Woven")) condition1 = "Woven";
                    else if (infoDto.sampleDescription.Contains("Knit")) condition1 = "Knit";
                    break;

            }
            return GetParameter(ItemName, condition, condition1);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Item, string? Condition, string? Condition1), string?> _map = new()
        {
            [("CF to Light", "general", null)] = "L-4",
            [("CF to Light", "swim wear", "neon&turquoise")] = "L-3",
            [("CF to Light", "swim wear", null)] = "L-5",
            [("CF to Light", "ski wear", null)] = "L-5",
            [("Water Resistance-Hydrostatic Pressure", "general", null)] = "1500 mmbar",
            [("Water Resistance-Hydrostatic Pressure", "ski wear", null)] = "3000 mmbar",
            [("Water Resistance-Hydrostatic Pressure", "sealed seam", null)] = "800 mmbar",
            [("Water Resistance-Hydrostatic Pressure", "non-sealed seam", null)] = "N/A",
            [("Pilling Resistance", "2000", null)] = "Cycle: 2000 revs",
            [("Pilling Resistance", "1000", null)] = "Cycle: 1000 revs",
            [("Pilling Resistance", "500", null)] = "Cycle: 500 revs",
            [("Attachment Strength", null, null)] = "Button(Individual fixing)：90N; Other：70N",
            [("Seam Slippage", null, null)] = "Normal Stressed Seams: 80N；Strong Stressed Seams: 100N",
            [("Tear Strengrh", null, null)] = "{Trousers, Skirts：15N；Blouses, Shirts, Dresses, Lining：8N；Other：12N}",
            [("Tensile Strengrh", null, null)] = "{Coats, Jackets, Slim fitted：150N； Blouses, Shirts, Dress, Lining：120N；Other：180N}",
        };

        private static string? GetParameter(string Item, string? Condition, string? Condition1)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((Item, Condition, Condition1), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((Item, Condition, null), out var fallback)) return fallback;

            return null!;
        }

    }
}
