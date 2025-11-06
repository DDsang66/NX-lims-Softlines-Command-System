using ClosedXML.Excel;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers
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
                ReportNumber = p.OrderNumber!,
                Temperature = IsA1MProgram(p.SampleDescription!) ? "40" : p.WashingProcedure.Contains("3") == true ? "30" : "40",
                Program = IsA1MProgram(p.SampleDescription!) ? "A1M" :
                p.WashingProcedure.Contains("3") == true ? "ref A2S" : "A2S",
                SteelBallNum = IsA1MProgram(p.SampleDescription!) ? 10 : _helper.IsCompositionExist("Animal", p.FiberContent!) == true ? 0 : 10
            },
            ("CF to Washing", "4H" or "3M" or "3G" or "3H", _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Temperature = IsA1MProgram(p.SampleDescription!) ? "40" : p.WashingProcedure.Contains("3") == true ? "30" : "40",
                Program = IsA1MProgram(p.SampleDescription!) ? "A1M" :
                p.WashingProcedure.Contains("3") == true ? "ref A2S" : "A2S",
                SteelBallNum = IsA1MProgram(p.SampleDescription!) ? 10 : 0
            },
            ("Absorbency", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                WashingProcedure = p.WashingProcedure,
                DryProcedure = p.DryProcedure,
                Temperature = p.WashingProcedure!.Contains("4") ? "40" : "30",
                Ballast = _helper.IsCompositionTypeExist("Cellulose", p.FiberContent!) >= 51 ? "Type I (100% Cotton)"
                : _helper.IsCompositionSourceExist("Synthetic", p.FiberContent!) >= 51 ? "Type III (100% Polyester)"
                : "Type III (100% Polyester)",
                AfterWash = "10",
                SpecialCareInstruction = p.Sci ?? null,
                IronMethod = p.IronMethod ?? null,
                Program = "900r"
            },
            ("DS to Washing", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
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
            },
            ("CF to Sublimation in Storage", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Temperature = p.SampleDescription!.Contains("Dyed") == true ? "90" : "70",
                Ballast = _helper.MaxComposition(p.FiberContent!)
            },
            ("CF to Hot Pressing", _, _) => new WetParameterIso
            {
                ContactItem = p.ItemName,
                ReportNumber = p.OrderNumber!,
                Temperature = p.IronMethod!.Contains("Cool") ? "110"
                : p.IronMethod!.Contains("Warm")?"150"
                : p.IronMethod!.Contains("Hot")?"200"
                :"/",
                Iron = Limitation("CF to Hot Pressing", p.SampleDescription!) == "L-5" ? "L-5" : null,
                IronMethod = p.IronMethod?? null,
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
            string? Limit = string.Empty;
            if (ItemName == "CF to Light")
            {
                Limit = GetLightLimitation(infoDto.sampleDescription!)!;
            }
            else if (ItemName == "CF to Water")
            {
                Limit = Limitation(ItemName, infoDto.sampleDescription!);
            }

            if (ItemName == "Extension and Recovery") 
            {
                var content = _helper.CompositionRate(infoDto.fiberComposition!, "Elastane");
                if (infoDto.sampleDescription!.Contains("Woven") ){ Limit = "Woven"; }
                else if (infoDto.sampleDescription!.Contains("Knit"))
                {
                    if (content <= 5)
                    { Limit = infoDto.sampleDescription.Contains("Strip")?"3": infoDto.sampleDescription.Contains("Loop")?"6":null; }
                    else if(content<12&&content>5)
                    {Limit = infoDto.sampleDescription.Contains("Strip") ? "4" : infoDto.sampleDescription.Contains("Loop") ? "8" : null;}
                    else if(content>=12&&content<=20)
                    {Limit = infoDto.sampleDescription.Contains("Strip") ? "5" : infoDto.sampleDescription.Contains("Loop") ? "10" : null;}
                    else if (content >20)
                    { Limit = infoDto.sampleDescription.Contains("Strip") ? "7" : infoDto.sampleDescription.Contains("Loop") ? "14" : null; }
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
            [("Regular(Tchibo)", "Pilling Resistance", null)] = "Cycle: 2000 revs",
            [("Regular(Tchibo)", "Air Permeability", null)] = "Area 20cm², P: 100Pa",
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
            else {
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
                    case "CF to Water": keywords.Add("148457");
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
                result = "20mL Wool Detergent";
            }
            else if (detergent == "Mild Detergent")
            {
                result = "20mL Mild Detergent";
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

        //l-5关键词列表
        private static readonly HashSet<string> L5Keywords = new HashSet<string>
            {
                "134404","135264","135264","135346","137387","137397","137398",
                "137398","137398","137398","137411","137421","137475","137636",
                "137672","137784","137792","137797","137800","138006","138023",
                "138181","138219","138325","138339","138368","138470","138475",
                "138615","138615","138712","138756","138758","138858","138868",
                "138881","138889","138904","138940","139049","139056","139304",
                "139321","139353","139360","139365","139407","139408","139435",
                "139436","139439","139448","139472","139489","139547","139556",
                "139574","139699","139719","139750","139803","139803","139852",
                "139853","139857","139864","139865","139878","139891","139928",
                "140083","140083","140084","140203","140206","140243","140293",
                "140301","140343","140357","140390","140391","140537","140568",
                "140568","140594","140642","140651","140653","140693","140693",
                "140696","140696","140746","140746","140747","140897","141135",
                "141145","141168","141168","141168","141187","141208","141335",
                "141921","141925","141999","142001","142004","142005","142016",
                "142022","142120","142120","142138","142138","142138","142140",
                "142157","142181","142255","142256","142293","142517","142605",
                "142608","142656","142657","142658","142659","142686","142713",
                "142817","142982","142983","143080","143106","143144","143196",
                "143217","143367","143373","143379","143415","143416","143417",
                "143428","143455","143456","143460","143461","143462","143468",
                "143482","143483","143547","143559","143560","143561","143585",
                "143585","143594","143611","143614","143713","143749","143801",
                "143828","143857","143896","143902","143944","143958","143992",
                "144047","144048","144054","144061","144071","144083","144097",
                "144126","144128","144138","144181","144202","144215","144220",
                "144234","144235","144238","144244","144245","144253","144254",
                "152060", "144255","144256","144260","144287","144320","144325",
                "144325","144440","144449","144475","144477","144525","144525",
                "154177","155964","154391","153231",
                "154147","144550","144553","144588","144616","144633","144635","144639","144642","144642","144646","144649","144658","144669","144704","144720","144767","144767","144768","144768","144778","144792","144792","144804","144821","144823","144825","144835","144837","144843","144848","144854","144859","144889","144890","144911","144989","144994","144995","144996","144998","144999","145001","145015","145017","145058","145137","145184","145188","145198","145199","145213","145239","145240","145241","145242","145243","145245","145246","145247","145266","145294","145295","145297","145301","145310","145311","145316","145316","145364","145386","145394","145394","145422","145424","145434","145444","145451","145462","145462","145467","145479","145485","145490","145519","145542","145716","145717","145718","145719","145724","145726","145727","145764","145777","145778","145779","145780","145798","145799","145799","145831","145840","145841","145842","145848","145850","145857","145865","145925","145927","145933","145934","145942","145994","145996","146141","146249","146250","146251","146252","146254","146256","146266","146276","146278","146298","146300","146308","146369","146373","146381","146392","146636","146638","146739","146740","146786","146789","146793","146796","146802","146810","146833","146955","146962","147018","147019","147021","147025","147026","147028","147040","147041","147043","147099","147099"

            };
        //l-3关键词列表
        private static readonly HashSet<string> L3Keywords = new HashSet<string>
            {
                "140693","140693","143209","143500","144481","144484",
                "144781","145138","147696","148144","148464","148905",
                "149137","150155","150505","151069","151171","151857"
            };
        //根据输入字符串判断Light Limitation等级
        private string? GetLightLimitation(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            if (input.Contains("148177"))
            {
                return input.Contains("Red") ? "L-4" : "L-3";
            }

            if (L5Keywords.Any(keyword => input.Contains(keyword)))
            {
                return "L-5";
            }

            if (L3Keywords.Any(keyword => input.Contains(keyword)))
            {
                return "L-3";
            }

            return "L-4";
        }
    }
}

