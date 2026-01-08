using ClosedXML.Excel;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider
{
    public class TchiboParamProvider
    {
        private readonly FiberContentHelper _helper;

        public TchiboParamProvider(FiberContentHelper helper)
        {
            _helper = helper;
        }
        //仅仅用于修改对应ItemName中的Parameter
        public WetParameterIso CreateWetParameters(ParamsInput p) => (p.ItemName, p.WashingProcedure, p.DCProcedure) switch
        {
            ("CF to Washing", "4N" or "4M" or "4G" or "3N", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Temperature = IsA1MProgram(p.SampleDescription!) ? "40" : p.WashingProcedure.Contains("3") == true ? "30" : "40",
                Program = IsA1MProgram(p.SampleDescription!) ? "A1M" :
                p.WashingProcedure.Contains("3") == true ? "ref A2S" : "A2S",
                SteelBallNum = IsA1MProgram(p.SampleDescription!) ? 10 : _helper.IsCompositionExist("Animal", p.FiberContent!) == true ? 0 : 10
            },
            ("CF to Washing", "4H" or "3M" or "3G" or "3H", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Temperature = IsA1MProgram(p.SampleDescription!) ? "40" : p.WashingProcedure.Contains("3") == true ? "30" : "40",
                Program = IsA1MProgram(p.SampleDescription!) ? "A1M" :
                p.WashingProcedure.Contains("3") == true ? "ref A2S" : "A2S",
                SteelBallNum = IsA1MProgram(p.SampleDescription!) ? 10 : 0
            },
            ("Absorbency", _, _) => new WetParameterIso
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
                AfterWash = "10",
                Detergent = GetDetergent(p.SampleDescription!, p.Detergent),
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Program = "900r"
            },
            ("Water Repellency-Spray Test", _, _) => new WetParameterIso
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
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Detergent = GetDetergent(p.SampleDescription!, p.Detergent),
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Program = "900r"
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
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Detergent = GetDetergent(p.SampleDescription!, p.Detergent),
                SpecialCareInstruction = p.Sci ?? null,
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
                Program = "900r"
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
                AfterWash = p.AfterWash?.Any() == true ? string.Join(",", p.AfterWash) : null,
                Detergent = GetDetergent(p.SampleDescription!, p.Detergent),
                SpecialCareInstruction = p.Sci ?? null,
                Program = "900r",
                Iron = p.Iron ?? null,
                IronMethod = p.IronMethod ?? null,
            },
            ("CF to Sublimation in Storage", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Temperature = "120",
                Ballast = _helper.MaxComposition(p.FiberContent!)
            },
            ("CF to Hot Pressing", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!,
                Temperature = p.IronMethod!.Contains("Cool") ? "110"
                : p.IronMethod!.Contains("Warm") ? "150"
                : p.IronMethod!.Contains("Hot") ? "200"
                : "/",
                Iron = Limitation("CF to Hot Pressing", p.SampleDescription!) == "L-5" ? "L-5" : null,
                IronMethod = p.IronMethod ?? null,
            },
            _ => new WetParameterIso
            {
                ContactItem = p.ItemName,
                Standard = p.Standard,
                ReportNumber = p.OrderNumber!
            }
        };


        public async Task<string?> CreateParameters([FromBody] RequiredInfoDto infoDto, string ItemName, string Standard)
        {

            // 1. 计算最大值
            string? Limit = string.Empty;
            if (ItemName == "CF to Light")
            {
                Limit = GetLightLimitation(infoDto.sampleDescription!)!;
            }
            else if (ItemName == "CF to Water")
            {
                Limit = Limitation(ItemName, infoDto.sampleDescription!);
            }
            else if (ItemName == "Pilling Resistance")
            {
                if (Standard!.Contains("12945-2")) Limit = "Woven";
                else if (Standard.Contains("12945-1")) Limit = "Knit";
                else Limit = "Woven";
            }
            if (ItemName == "Extension and Recovery")
            {
                var content = _helper.CompositionRate(infoDto.fiberComposition!, "Elastane");
                if (content == 0) Limit = "N/A";
                if (infoDto.sampleDescription!.Contains("Woven")) { Limit = "Woven"; }
                else if (infoDto.sampleDescription!.Contains("Knit"))
                {
                    if (content <= 5)
                    { Limit = infoDto.sampleDescription.Contains("Stripe") ? "3" : infoDto.sampleDescription.Contains("Loop") ? "6" : null; }
                    else if (content < 12 && content > 5)
                    { Limit = infoDto.sampleDescription.Contains("Stripe") ? "4" : infoDto.sampleDescription.Contains("Loop") ? "8" : null; }
                    else if (content >= 12 && content <= 20)
                    { Limit = infoDto.sampleDescription.Contains("Stripe") ? "5" : infoDto.sampleDescription.Contains("Loop") ? "10" : null; }
                    else if (content > 20)
                    { Limit = infoDto.sampleDescription.Contains("Stripe") ? "7" : infoDto.sampleDescription.Contains("Loop") ? "14" : null; }
                }
            }


            string menuName = infoDto.menuName!;
            if (menuName == null) { return null; }
            // 2. 根据 Menu/Item 组合查表
            return GetParameter(menuName, ItemName, Limit);//返回一个string类型的Parameter
        }

        // ---------- 2. 映射表 ----------
        private static readonly Dictionary<(string Menu, string Item, string? Lv), string?> _map = new()
        {
            //CF to Water有一个五级
            [("Regular(Tchibo)", "CF to Light", "L-5")] = "L-5",
            [("Regular(Tchibo)", "CF to Light", "L-4")] = "L-4",
            [("Regular(Tchibo)", "CF to Light", "L-3")] = "L-3",
            [("Regular(Tchibo)", "CF to Saliva", null)] = "L-5",
            [("Regular(Tchibo)", "CF to Sweat", null)] = "L-5",
            [("Regular(Tchibo)", "CF to Water", "L-5")] = "L-5",
            [("Regular(Tchibo)", "Seam Slippage", null)] = "Load: 16N",
            [("Regular(Tchibo)", "Appearance", null)] = "In house method",
            [("Regular(Tchibo)", "Pilling Resistance", "Woven")] = "Cycle: 2000 revs",
            [("Regular(Tchibo)", "Pilling Resistance", "Knit")] = "Cycle: Due to Requirement",
            [("Regular(Tchibo)", "Air Permeability", null)] = "Area 20cm², P: 100Pa",
            [("Regular(Tchibo)", "Extension and Recovery", "N/A")] = "N/A",
            [("Regular(Tchibo)", "Extension and Recovery", "Woven")] = "Load: 30N,Cycle: 5",
            [("Regular(Tchibo)", "Extension and Recovery", "3")] = "Load: 3N,Cycle: 5",
            [("Regular(Tchibo)", "Extension and Recovery", "4")] = "Load: 4N,Cycle: 5",
            [("Regular(Tchibo)", "Extension and Recovery", "5")] = "Load: 5N,Cycle: 5",
            [("Regular(Tchibo)", "Extension and Recovery", "6")] = "Load: 6N,Cycle: 5",
            [("Regular(Tchibo)", "Extension and Recovery", "7")] = "Load: 7N,Cycle: 5",
            [("Regular(Tchibo)", "Extension and Recovery", "8")] = "Load: 8N,Cycle: 5",
            [("Regular(Tchibo)", "Extension and Recovery", "10")] = "Load: 10N,Cycle: 5",
            [("Regular(Tchibo)", "Extension and Recovery", "14")] = "Load: 14N,Cycle: 5",
        };

        private static string? GetParameter(string menu, string item, string? lv)
        {
            // 1) 先精确匹配 (Menu, Item, Lv)
            if (_map.TryGetValue((menu, item, lv), out var exact)) return exact;

            // 2) 再匹配 (Menu, Item, any)
            if (_map.TryGetValue((menu, item, null), out var fallback)) return fallback;

            return null!;
        }

        private string? Limitation(string item, string sampleDescription)
        {
            if (sampleDescription == null) return null;
            // 定义一个字典，包含需要检查的值
            if (string.IsNullOrEmpty(item)) return null;
            else
            {
                HashSet<string> keywords = new HashSet<string>();
                switch (item)
                {
                    case "CF to Hot Pressing":
                        keywords.UnionWith(new[]
                        {
                            "138827", "139556", "140203", "140206", "140537", "140642", "143196",
                            "143547", "143828", "144220", "144475", "144481", "144781", "145138",
                            "145777", "145778", "145933", "147338", "147696", "148481", "148916",
                            "149546", "151069", "151518", "152076"});
                        break;
                    case "CF to Water":
                        keywords.Add("148457");
                        break;
                }
                // 遍历字典中的每个值
                foreach (string keyword in keywords)
                {
                    // 如果 sampleDescription 包含字典中的某个值
                    if (sampleDescription.Contains(keyword))
                    {
                        return "L-5"; // 返回 true
                    }
                }
            }

            return null;
        }

        private bool IsA1MProgram(string sampleDescription)
        {
            if (sampleDescription == null) return false;
            // 定义一个字典，包含需要检查的值
            HashSet<string> keywords = new HashSet<string>
            {
                "138017", "139845", "138023", "138696", "138829", "138880", "138916",
                "139154", "139312", "139532", "139848", "139849", "139851", "139907",
                "139908", "140381", "140393", "140395", "141175", "141258"
            };

            // 遍历字典中的每个值
            foreach (string keyword in keywords)
            {
                // 如果 sampleDescription 包含字典中的某个值
                if (sampleDescription.Contains(keyword))
                {
                    return true; // 返回 true
                }
            }
            return false;
        }
        //判断洗涤剂
        private string GetDetergent(string SampleDescription, string? detergent)
        {
            var result = string.Empty;
            if (detergent == "Wool Detergent")
            {
                result = "20mL Woolite Detergent";
            }
            else if (detergent == "Mild Detergent")
            {
                result = "20mL Coral Detergent";
            }
            else if (string.IsNullOrEmpty(detergent))
            {
                if (SampleDescription == null) return string.Empty;
                if (SampleDescription.Contains("White"))
                {
                    result = "40g Persil Powder";
                }
                else
                {
                    result = "40g Ariel Powder";
                }
            }
            return result;
        }


        //根据输入字符串判断Light Limitation等级
        private string? GetLightLimitation(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            return input.Contains("L-5")?"L-5" :
                   input.Contains("L-4") ? "L-4" :
                   input.Contains("L-3") ? "L-3" :
                   null;
        }
    }
}

