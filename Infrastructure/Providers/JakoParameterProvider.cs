using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using SixLabors.Fonts.Unicode;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers
{
    public class JakoParameterProvider
    {
        private readonly FiberContentHelper _helper;

        public JakoParameterProvider(FiberContentHelper helper)
        {
            _helper = helper;
        }

        public async Task<WetParameterIso> CreateWetParameters(ParamsInput p) => (p.ItemName, p.sampleDescription) switch
        {
            ("CF to Washing", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Temperature = p.sampleDescription!.Contains("Fabric") == true ? "40" : null,
                Program = p.sampleDescription.Contains("Fabric") == true ? "A2S" : null,
                SteelBallNum = _helper.IsCompositionExist("Animal", p.FiberContent!) == true ? 0 : 10
            },
            ("Appearance", var add) when (add!.Contains("Fabric") || add.Contains("Components")) == true => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = "4N",
                DryProcedure = "Tumble Dry",
                Temperature = "40",
                AfterWash = p.sampleDescription!.Contains("1 Wash") == true ? 1 : 5,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!)
                + _helper.IsCompositionSourceExist("Vegetable", p.FiberContent!)
                + _helper.IsCompositionSourceExist("Man-made", p.FiberContent!) >= 51 ? "Type I (100% cotton)" : "Type III (100% polyester)"
            },
            ("Appearance", var add) when add?.Contains("Garment") == true => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = _helper.MaxComposition(p.FiberContent!) == "Cotton" ? "Cotton procedure" : "Minimum iron procedure",
                DryProcedure = (p.sampleDescription!.Contains("Rain") || p.sampleDescription.Contains("Padding") || p.sampleDescription.Contains("Down Jackets")) == true ? p.DCProcedure : "Tumble Dry",
                Temperature = (p.sampleDescription!.Contains("Rain") || p.sampleDescription.Contains("Padding") || p.sampleDescription.Contains("Down Jackets")) == true ? p.WashingProcedure!.Contains("4") ? "40" : "30" : "40",
                AfterWash = p.sampleDescription!.Contains("1 Wash") == true ? 1 : 5,
                Program = _helper.MaxComposition(p.FiberContent!) == "Cotton" ? "1400 rpm, automatic time 1:50h"
                : await _helper.MaxCompositionType(p.FiberContent!) == "Synthetic" ? "1200 rpm, automatic time 1:20h"
                : "600 rpm 1h for mild wash"
            },
            ("Spriality/Skewing", var add) when (add!.Contains("Fabric") || add.Contains("Components")) == true => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = "4N",
                DryProcedure = "Tumble Dry",
                Temperature = "40",
                AfterWash = p.sampleDescription!.Contains("1 Wash") == true ? 1 : 5,
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!)
                + _helper.IsCompositionSourceExist("Vegetable", p.FiberContent!)
                + _helper.IsCompositionSourceExist("Man-made", p.FiberContent!) >= 51 ? "Type I (100% cotton)" : "Type III (100% polyester)"
            },
            ("Spriality/Skewing", var add) when add?.Contains("Garment") == true => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = _helper.MaxComposition(p.FiberContent!) == "Cotton" ? "Cotton procedure" : "Minimum iron procedure",
                DryProcedure = (p.sampleDescription!.Contains("Rain") || p.sampleDescription.Contains("Padding") || p.sampleDescription.Contains("Down Jackets")) == true ? p.DCProcedure : "Tumble Dry",
                Temperature = (p.sampleDescription!.Contains("Rain") || p.sampleDescription.Contains("Padding") || p.sampleDescription.Contains("Down Jackets")) == true ? p.WashingProcedure!.Contains("4") ? "40" : "30" : "40",
                AfterWash = p.sampleDescription!.Contains("1 Wash") == true ? 1 : 5,
                Program = _helper.MaxComposition(p.FiberContent!) == "Cotton" ? "1400 rpm, automatic time 1:50h"
                : await _helper.MaxCompositionType(p.FiberContent!) == "Synthetic" ? "1200 rpm, automatic time 1:20h"
                : "600 rpm 1h for mild wash"
            },
            ("DS to Dry-clean", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Sensitive = (p.DCProcedure == "DC Normal" || p.DCProcedure == "Petroleum DC Normal") && _helper.IsCompositionExist("Animal", p.FiberContent!) == true ||
                                  p.DCProcedure == "DC Sensitive" || p.DCProcedure == "Petroleum DC Sensitive" ? "Y" : "N"
            },
            ("Heat Press Test For JAKO", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Temperature = p.sampleDescription!.Contains("Sublimation") == true ? "130" : "170",
                Program = "30",
            },
            ("Print Durability For JAKO", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Temperature = "60",
                Program = "1200 rpn, automatic time 1:20h",
                DryProcedure = "Tumble Dry",
                AfterWash = p.sampleDescription!.Contains("Repeat Order") == true ? 10 : 20
            },
            ("CF to Hot Pressing", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Temperature = p.sampleDescription!.Contains("Dyed") == true ? "150" : "110",
            },
            ("CF to Sublimation in Storage", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Temperature = p.sampleDescription!.Contains("Dyed") == true ? "90" : "70",
            },
            _ => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!
            }
        };

        public async Task<string?> CreateParameters([FromBody] RequiredInfoDto infoDto, string ItemName)
        {

            // 最大成分
            string? largestVarName = _helper.MaxComposition(infoDto.fiberComposition!)!;
            string? largestVarType = await _helper.MaxCompositionType(infoDto.fiberComposition!)!;
            string menuName = infoDto.menuName!;
            if (menuName == null) { return null; }
            // 2. 根据 Menu/Item 组合查表
            return GetParameter(menuName, ItemName, largestVarName, largestVarType);//返回一个string类型的Parameter
        }


        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Menu, string Item, string? Lv, string? Lt), string?> _map = new()
        {
            [("RegularFabric(JAKO)", "Pilling Resistance", "Cotton", "Vegetable")] = "Cycle: 3000r",
            [("RegularFabric(JAKO)", "Pilling Resistance", "Polyester", "Synthetic")] = "Cycle: 7000r",
            [("RegularFabric(JAKO)", "Pilling Resistance", "Cotton", "Synthetic")] = "Cycle: 7000r",
            [("RegularFabric(JAKO)", "Pilling Resistance", null, null)] = null,
            [("RegularFabric(JAKO)", "Seam Slippage", null, null)] = "Load: 16N",
            [("RegularFabric(JAKO)", "CF to Light", null, null)] = "L-4",
            [("RegularFabric(JAKO)", "Bursting Strength", null, null)] = "Diameter: 30.5mm (Test Area: 7.3cm²)",
            [("RegularFabric(JAKO)", "Tensile Strength", null, null)] = "Gauge: 75mm",
            [("RegularFabric(JAKO)", "Extension and Recovery", null, null)] = "Load: 3daN,Cycle: 5",
            [("RegularFabric(JAKO)", "Air Permeability", null, null)] = "Area 20cm², P: 100Pa",
            [("RegularFabric(JAKO)", "Snagging Resistance", null, null)] = "Cycle: 600r",
            [("RegularFabric(JAKO)", "Abrasion Resistance", null, null)] = "Load: 9KPa,Cycle: 30000r",
            [("RegularFabric(JAKO)", "Water Repellency-Spray Test", null, null)] = "Original & After wash",
            [("RegularFabric(JAKO)", "Spriality/Skewing", null, null)] = "Same as Appearance",
            [("RegularFabric(JAKO)", "CF to Chlorinated Water", null, null)] = "Confirmation with CS is required",
            [("RegularGarment(JAKO)", "Seam Strength", null, null)] = "Knit Structure Change Into Seam Bursting; Test Area:50cm²",
        };

        private static string? GetParameter(string menu, string item, string? lv, string? lt)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((menu, item, lv, lt), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((menu, item, null, null), out var fallback)) return fallback;

            return null!;
        }
    }
}
