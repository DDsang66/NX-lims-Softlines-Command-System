using OfficeOpenXml;
using static NX_lims_Softlines_Command_System.Application.Services.Factory.PrintExcelStrategyFactory;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.Helper;
using DocumentFormat.OpenXml.Spreadsheet;
using System.ComponentModel;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelPrintTool;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.PrintExcelMethod
{

    public sealed class PrintFocusExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintFocusExcel(LabDbContextSec db)
        {
            _db = db;
        }

        public void PrintJsonData(ExcelSubmitDto Dto, ExcelPackage PackageWet, ExcelPackage PackagePhy)
        {
            string reportNumber = Dto.ReportNumber!;
            string buyer = Dto.Buyer!;
            string menu = Dto.MenuName!;
            string sampleDescription = Dto.SampleDescription!;
            var selectedRows = Dto.SelectedRows;

            List<CheckListDto> checkLists = new List<CheckListDto>();
            foreach (var row in selectedRows!) checkLists.Add(new CheckListDto().CreateDto(row, menu, sampleDescription));
            foreach (var dto in checkLists)
            {
                Console.WriteLine($"{dto.ItemName} -> {dto.Type}");
                var pkg = dto.Type == "Wet" ? PackageWet : PackagePhy;
                if (TemplateSheetNames.ContainsKey(dto.ItemName!) || TemplateSheetNamesNormal.ContainsKey(dto.ItemName!))
                    FillSheet(pkg, dto.ItemName!, dto, reportNumber);
            }
            PackageWet.Save();
            PackagePhy.Save();

        }

        private void FillSheet(
            ExcelPackage pkg,
            string itemName,
            CheckListDto dto,
            string reportNo)
        {
            var tplName = new TemplateSelector(TemplateSheetNames, TemplateSheetNamesNormal).GetTemplateName(itemName, dto.sampleDescription!);
            var template = pkg.Workbook.Worksheets[tplName];
            // 2) 计算需要几张 sheet
            var cellAddrs = CellMapper[itemName](itemName, dto.sampleDescription!,dto.Standard!);
            string[]? AfterWashCellAddrs = null;
            if (itemName == "DS to Washing" || itemName == "Spirality/Skewing" || itemName == "Appearance")
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName, dto.sampleDescription!);
            }


            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();


            int[]? afterWashMap = null;
            if (itemName == "DS to Washing"  || itemName == "Spirality/Skewing" || itemName == "Appearance")
            {
                var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                if (wp == null) wp = new WetParameterIso();
                string? afterWash = wp!.AfterWash;
                string? iron = wp!.Iron;
                string? ironMethod = wp!.IronMethod;
                samples = SampleNumCounter.GetSample(dto.Sample!, afterWash, iron, ironMethod);
                afterWashMap = SampleNumCounter.ExpandWashNumbers(samples!, afterWash!,iron);
            }
            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            int offset = 0; // 假设没有偏移
            offset = OffsetRule.GetValueOrDefault(itemName, 0);
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            if (itemName == "Water Repellency-Spray Test") { capacity = 3; }
            if (itemName == "DS to Washing"&&! dto.sampleDescription!.Contains("Fabric")) { capacity = 1; }
            int sheetCnt = (int)Math.Ceiling(samples!.Length / (double)capacity);


            List<ExcelWorksheet> sheets = new List<ExcelWorksheet>();
            for (int idx = 0; idx < sheetCnt; idx++)
            {
                ExcelWorksheet ws;
                if (idx == 0)
                {
                    ws = template; // 第一张用模板
                }
                else
                {
                    string newSheetName = $"{tplName} ({idx + 1})";
                    // 检查是否已经存在同名的 sheet
                    if (pkg.Workbook.Worksheets.Any(ws => ws.Name == newSheetName))
                    {
                        ws = pkg.Workbook.Worksheets[newSheetName];
                    }
                    else
                    {
                        ws = pkg.Workbook.Worksheets.Copy(tplName, newSheetName);
                    }
                }
                sheets.Add(ws);
            }
            //先复制后写入
            for (int idx = 0; idx < sheetCnt; idx++)
            {
                //这里是分割样本的逻辑<-------------------------------------------------------------------------------------->
                ExcelWorksheet ws = sheets[idx];
                /* 计算当前 sheet 要写的样本区间 */
                int start = idx * capacity;                         // 本 sheet 起始样本索引
                int end = Math.Min(start + capacity, samples.Length);
                int count = end - start;                            // 本 sheet 要写的样本数量
                if (count <= 0) continue;
                /* 取本 sheet 对应的那段样本 */
                string[] slice = samples.Skip(start).Take(count).ToArray();
                int[]? afmap = null;
                if (afterWashMap != null) afmap = afterWashMap.Skip(start).Take(count).ToArray();
                /* 把这段样本写进去,如果有水洗遍数，那么也把水洗遍数写进去 */
                WriteSamples(ws, slice, afmap, cellAddrs, AfterWashCellAddrs, itemName,dto.sampleDescription!);
                //这里是分割样本的逻辑<-------------------------------------------------------------------------------------->
                // 5) 其余参数
                if (dto.Type == "Wet")
                {
                    var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                    var extraMap = WetExtraMap.GetValueOrDefault(itemName, (wp, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>())(wp!, dto, reportNo);

                    foreach (var kv in extraMap)
                    {
                        // 如果 wp 为 null，提供一个默认值或者跳过某些操作
                        if (wp == null)
                        {
                            var defaultWp = new WetParameterIso();
                            ws.Cells[kv.Key].Value = kv.Value(defaultWp, dto, reportNo);
                        }
                        else
                        {
                            ws.Cells[kv.Key].Value = kv.Value(wp, dto, reportNo);
                        }
                    }
                }
                else if (dto.Type == "Physics")
                {
                    var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                    var extraMap = PhyExtraMap.GetValueOrDefault(itemName, (wp,dto, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, string, string>>())(wp,dto, reportNo);
                    foreach (var kv in extraMap)
                    {
                        // 如果 wp 为 null，提供一个默认值或者跳过某些操作
                        if (wp == null)
                        {
                            var defaultWp = new WetParameterIso();
                            ws.Cells[kv.Key].Value = kv.Value(defaultWp, dto, reportNo);
                        }
                        else
                        {
                            ws.Cells[kv.Key].Value = kv.Value(wp, dto, reportNo);
                        }
                    }
                }
            }


        }
        private static readonly Dictionary<string, string> TemplateSheetNamesNormal = new()
        {
            ["Weight"] = "Weight",
            ["Yarn Count"] = "Yarn Count",
            ["Twist"] = "Yarn Twist",
            ["Width"] = "FabricWidth",
            ["Density"] = "Density",
            ["Fabric Construction(Weave)"] = "Weave",
            ["Bow and Skew"] = "Bow&Skew&Thickness",
            ["Thickness"] = "Bow&Skew&Thickness",
            ["Electrostatic Properties"]= "Electrostatic Properties",
            ["Drying Rate of Fabrics"] = "DryingRate",
            ["Pilling Resistance"] = "Pilling Resistance",
            ["Abrasion Resistance"] = "Abrasion&Snagging",
            ["Snagging Resistance"] = "Abrasion&Snagging",
            ["Zipper Strength"] = "Zipper Strength",
            ["Tear Strength"] = "Tearing Strength",
            ["Tensile Strength"] = "Tensile Strength",
            ["Bonding Strength"] = "Peel Bond",
            ["Bursting Strength"] = "Bursting Strength",
            ["Attachment Strength"] = "Attachment Strength",
            ["Seam Slippage"] = "Seam Slippage",
            ["Absorbency"] = "Absorbency",
            ["Water Resistance-Hydrostatic Pressure"] = "Hydrostatic",
            ["Water Repellency-Spray Test"] = "Water Repellency",
            ["Extension and Recovery"] = "Stretch&Recovery of Elastic",

            ["Appearance"] = "AppearanceAfterWashing",
            ["Appearance after Accelerated Heat Aging"] = "AppearanceAfterAging",
            ["Appearance of Smoothness"] = "Smoothness Appearance",
            ["CF to Chlorinated Water"] = "CFtoSaliva&Sweat",
            ["DS to Dry-clean"] = "DStoDryClean",
            ["Dimensional Stability to Ironing"] = "DStoIroning",
            ["DS to Steaming"] = "DStoSteam",
            ["Spirality/Skewing"] = "Spirality",
            ["CF to Washing"] = "CFtoWRL",
            ["CF to Rubbing"] = "CFtoWRL",
            ["CF to Light"] = "CFtoWRL",
            ["CF to Sea Water"] = "CFtoPWS",
            ["CF to Perspiration"] = "CFtoPWS",
            ["CF to Water"] = "CFtoPWS",
            ["CF to Dry-clean"] = "CFtoYD",
            ["CF to Saliva"] = "CFtoSaliva&Sweat",
            ["CF to Sweat"] = "CFtoSaliva&Sweat",
            ["CF to Chlorinated Water"] = "CFtoCl&Bleach",
            ["Phenolic Yellowing"] = "CFtoYD",
        };
        private static readonly Dictionary<string, Dictionary<string, string>> TemplateSheetNames = new()
        {
            ["DS to Washing"] = new Dictionary<string, string>
            {
                {"Fabric", "DStoWashing-F" },
                {"Garment","DStoWashing-G"},
            },
        };
        private static readonly Dictionary<string, Func<string, string, string,string[]>> CellMapper = new()
        {
            ["Weight"] = (n, m,l) => ExcelFocusMapper.MapWeight(),
            ["Yarn Count"] = (n, m, l) => ExcelFocusMapper.MapYarnCount(),
            ["Twist"] = (n, m, l) => ExcelFocusMapper.MapYarnTwist(),
            ["Width"] = (n, m, l) => ExcelFocusMapper.MapWidth(),
            ["Density"] = (n, m, l) => ExcelFocusMapper.MapDensity(),
            ["Fabric Construction(Weave)"] = (n, m, l) => ExcelFocusMapper.MapWeave(),
            ["Bow and Skew"] = (n, m, l) => ExcelFocusMapper.MapBowSkew(),
            ["Thickness"] = (n, m, l) => ExcelFocusMapper.MapThickness(),
            ["Electrostatic Properties"] = (n, m, l) => ExcelFocusMapper.MapElect(),
            ["Drying Rate of Fabrics"] = (n, m, l) => ExcelFocusMapper.MapDryRate(),
            ["Pilling Resistance"] = (n, m, l) => ExcelFocusMapper.MapPilling(),
            ["Abrasion Resistance"] = (n, m, l) => ExcelFocusMapper.MapAbrasion(),
            ["Snagging Resistance"] = (n, m, l) => ExcelFocusMapper.MapSnagging(),
            ["Zipper Strength"] = (n, m, l) => ExcelFocusMapper.MapZipperStrength(),
            ["Tear Strength"] = (n, m, l) => ExcelFocusMapper.MapTear(),
            ["Tensile Strength"] = (n, m, l) => ExcelFocusMapper.MapTensile(),
            ["Bonding Strength"] = (n, m, l) => ExcelFocusMapper.MapBond(),
            ["Bursting Strength"] = (n, m, l) => ExcelFocusMapper.MapBursting(),
            ["Attachment Strength"] = (n, m, l) => ExcelFocusMapper.MapAttachmentStrength(),
            ["Seam Slippage"] = (n, m, l) => ExcelFocusMapper.MapSeam(l),
            ["Absorbency"] = (n, m, l) => ExcelFocusMapper.MapAbsorbency(),
            ["Water Resistance-Hydrostatic Pressure"] = (n, m, l) => ExcelFocusMapper.MapHydrostatic(),
            ["Water Repellency-Spray Test"] = (n, m, l) => ExcelFocusMapper.MapRepellency(m),
            ["Extension and Recovery"] = (n, m, l) => ExcelFocusMapper.MapExtensionAndRecovery(),

            ["Appearance"] = (n, m, l) => ExcelFocusMapper.MapAppearance(),
            ["Appearance after Accelerated Heat Aging"] = (n, m, l) => ExcelFocusMapper.MapAging(),
            ["Appearance of Smoothness"] = (n, m, l) => ExcelFocusMapper.MapSmoothnessAppearance(),
            ["CF to Chlorinated Water"] = (n, m, l) => ExcelFocusMapper.MapCFtoCl(),
            ["DS to Dry-clean"] = (n, m, l) => ExcelFocusMapper.MapExtensionAndRecovery(),
            ["Dimensional Stability to Ironing"] = (n, m, l) => ExcelFocusMapper.MapDStoIron(),
            ["DS to Steaming"] = (n, m, l) => ExcelFocusMapper.MapDStoSteam(),
            ["Spirality/Skewing"] = (n, m, l) => ExcelFocusMapper.MapSpirality(),
            ["CF to Washing"] = (n, m, l) => ExcelFocusMapper.MapCFtoWashing(),
            ["CF to Rubbing"] = (n, m, l) => ExcelFocusMapper.MapCFtoRubbing(),
            ["CF to Light"] = (n, m, l) => ExcelFocusMapper.MapCFtoLight(),
            ["CF to Sea Water"] = (n, m, l) => ExcelFocusMapper.MapCFtoSeaWater(),
            ["CF to Perspiration"] = (n, m, l) => ExcelFocusMapper.MapCFtoPerspiration(),
            ["CF to Water"] = (n, m, l) => ExcelFocusMapper.MapCFtoWater(),
            ["CF to Dry-clean"] = (n, m, l) => ExcelFocusMapper.MapCFtoDC(),
            ["CF to Saliva"] = (n, m, l) => ExcelFocusMapper.MapCFtoSalivaSweat(),
            ["CF to Sweat"] = (n, m, l) => ExcelFocusMapper.MapCFtoSalivaSweat(),
            ["Phenolic Yellowing"] = (n, m, l) => ExcelFocusMapper.MapCFtoYellow(),
            ["DS to Washing"] = (n, m, l) => ExcelFocusMapper.MapDStoWashing(m),
        };
        //取洗涤遍数映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> AfterWashCellMapper = new()
        {
            ["DS to Washing"] = (_, m) => ExcelFocusMapper.DStoWashingAf(m),
            ["DS to Dry-clean"] = (_, m) => ExcelFocusMapper.DStoDCAf(),
            ["Appearance"] = (_, m) => ExcelFocusMapper.AppearanceAf(),
            ["Spirality/Skewing"] = (_, m) => ExcelFocusMapper.SpiralityAf(),
        };
        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, string, Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>>> WetExtraMap = new()
        {

            ["DS to Washing"] = (w, dto, reportNo) =>
               {
                   var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                   if (dto.sampleDescription!.Contains("Fabric"))
                   {
                       map["BC1"] = (w, dto, reportNo) => reportNo;
                       map["AR3"] = (w, dto, reportNo) => (dto.Standard ?? "").Replace(",", " / ").TrimEnd(' ', '/');
                       map["AX4"] = (w, dto, reportNo) => w.WashingProcedure!;
                       map["BX4"] = (w, dto, reportNo) => w.Temperature!;
                       map["BG5"] = (w, dto, reportNo) => w.Ballast!;
                       map["BI6"] = (w, dto, reportNo) => w.DryProcedure!;
                       map["BR6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                       map["AR7"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                   }
                   else if (dto.sampleDescription!.Contains("Garment"))
                   {
                       map["P1"] = (w, dto, reportNo) => reportNo;
                       map["A3"] = (w, dto, reportNo) => (dto.Standard ?? "").Replace(",", " / ").TrimEnd(' ', '/');
                       map["I4"] = (w, dto, reportNo) => w.WashingProcedure!;
                       map["AJ4"] = (w, dto, reportNo) => w.Temperature!;
                       map["S5"] = (w, dto, reportNo) => w.Ballast!;
                       map["V6"] = (w, dto, reportNo) => w.DryProcedure!;
                       map["AE6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                       map["A7"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                   }
                   else
                   {
                       map["N1"] = (w, dto, reportNo) => reportNo;
                       map["A3"] = (w, dto, reportNo) => dto.Standard!;
                       map["G4"] = (w, dto, reportNo) => w.WashingProcedure!;
                       map["AL4"] = (w, dto, reportNo) => w.Temperature!;
                       map["R5"] = (w, dto, reportNo) => w.Ballast!;
                       map["T6"] = (w, dto, reportNo) => w.DryProcedure!;
                       map["AD6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                       map["A7"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction) == true ? "-" : w.SpecialCareInstruction;
                   }
                   return map;
               },
            ["Appearance"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
                ["AR38"] = (w, dto, reportNo) => "√"!,
                ["BI13"] = (w, dto, reportNo) => w.IronMethod!,
                ["AX39"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["BX39"] = (w, dto, reportNo) => w.Temperature!,
                ["BG40"] = (w, dto, reportNo) => w.Ballast!,
                ["BI41"] = (w, dto, reportNo) => w.DryProcedure!,
                ["BR41"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!,
                ["AR42"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!
            },
            ["Appearance after Accelerated Heat Aging"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
            },
            ["Appearance of Smoothness"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR4"] = (w, dto, reportNo) => dto.Standard!,
                ["AT6"] = (w, dto, reportNo) => "1",
                ["AT13"] = (w, dto, reportNo) => "1"
            },
            ["CF to Chlorinated Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["H1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["E4"] = (w, dto, reportNo) => "20"
            },
            ["CF to Washing"]= (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>> 
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["B4"] = (w, dto, reportNo) => w.Program!,
                ["E4"] = (w, dto, reportNo) => w.Temperature!,
                ["L5"] = (w, dto, reportNo) => w.SteelBallNum!.ToString()!,
            },
            ["CF to Rubbing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A19"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Light"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A26"] = (w, dto, reportNo) => dto.Standard!,
                ["B29"] = (w, dto, reportNo) => dto.Parameter!.Contains("60 hours") ? "60h" : "20h",
            },
            ["CF to Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A27"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Phenolic Yellowing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Dry-clean"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR11"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Saliva"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["G3"] = (w, dto, reportNo) => "√"
            },
            ["CF to Sweat"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,       
                ["J3"] = (w, dto, reportNo) => "√"
            },
            ["CF to Sea Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A40"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Dimensional Stability to Ironing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["A40"] = (w, dto, reportNo) => dto.Standard!,
                ["BB4"] = (w, dto, reportNo) => w.IronMethod!
            },
            ["DS to Steaming"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["DS to Dry-clean"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) =>dto.Standard!,
                ["AW4"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal"
            },
            ["Spirality/Skewing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["P1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
        };
        private static readonly Dictionary<string, Func<WetParameterIso,CheckListDto, string, Dictionary<string, Func<WetParameterIso,CheckListDto, string, string>>>> PhyExtraMap = new()
        {
            ["Weight"]= (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Yarn Count"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Twist"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Density"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Width"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Fabric Construction(Weave)"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
            },
            ["Bow and Skew"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Thickness"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A33"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Drying Rate of Fabrics"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },

            ["Pilling Resistance"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (wp, dto, reportNo) => reportNo;
                map["F3"] = (w, dto, reportNo) => dto.Standard!;
                map["D4"] = (w, dto, reportNo) => "2000 revs";
                map["G21"] = (w, dto, reportNo) => w.WashingProcedure!;
                map["AJ21"] = (w, dto, reportNo) => w.Temperature!;
                map["Q22"] = (w, dto, reportNo) => w.Ballast!;
                map["L23"] = (w, dto, reportNo) => w.DryProcedure!;
                map["U23"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                map["A24"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                return map;
            },
            ["Abrasion Resistance"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.Standard!;
                if (dto.Parameter!.Contains("12KPa"))
                {
                    map["C5"] = (w, dto, reportNo) => "12KPa";
                    map["I5"] = (w, dto, reportNo) => "10000";
                    map["AA5"] = (w, dto, reportNo) => "-";
                }
                else 
                {
                    map["C5"] = (w, dto, reportNo) => "9KPa";
                    map["I5"] = (w, dto, reportNo) =>dto.Parameter!.Contains("10000 revs")?"10000":"5000";
                    map["AA5"] = (w, dto, reportNo) => "-";
                }
                return map;
            },
            ["Snagging Resistance"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["J26"] = (w, dto, reportNo) => dto.Standard!,
                ["C28"] = (w, dto, reportNo) => "600",
            },
            ["Zipper Strength"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Tear Strength"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (wp, dto, reportNo) => reportNo;
                map["F3"] = (wp, dto, reportNo) => dto.Standard!;
                if (w.WashingProcedure!.Contains("Hand") && dto.Parameter!.Contains("1 Wash"))
                {
                    map["Y26"] = (w, dto, reportNo) => w.Temperature!;
                    map["AE26"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["A27"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                else if (dto.Parameter!.Contains("1 Wash"))
                {
                    map["A24"] = (w, dto, reportNo) => w.Bleach + " Cycle";
                    map["AE23"] = (w, dto, reportNo) => w.Temperature!;
                    map["N23"] = (w, dto, reportNo) => w.Program!;
                    map["M24"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["T23"] = (w, dto, reportNo) => w.DryCleanProcedure!;
                    map["V24"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ " : w.IronMethod!;
                    map["A27"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                else if (dto.Parameter!.Contains("Dry-clean"))
                {
                    map["L29"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                return map;
            },
            ["Tensile Strength"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (wp, dto, reportNo) => reportNo;
                map["I3"] = (wp, dto, reportNo) => dto.Standard!;
                if (w.WashingProcedure!.Contains("Hand")&&dto.Parameter!.Contains("1 Wash"))
                {
                    map["Y27"] = (w, dto, reportNo) => w.Temperature!;
                    map["AE27"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["A28"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                else if(dto.Parameter!.Contains("1 Wash"))
                {
                    map["A25"] = (w, dto, reportNo) => w.Bleach + " Cycle";
                    map["AE24"] = (w, dto, reportNo) => w.Temperature!;
                    map["N24"] = (w, dto, reportNo) => w.Program!;
                    map["M25"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["T24"] = (w, dto, reportNo) => w.DryCleanProcedure!;
                    map["V25"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ " : w.IronMethod!;
                    map["A28"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                else if (dto.Parameter!.Contains("Dry-clean"))
                {
                    map["L30"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                return map;
            },
            ["Bursting Strength"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["I3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Extension and Recovery"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (wp, dto, reportNo) => reportNo;
                map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                map["AC7"] = (wp, dto, reportNo) => dto.Parameter!.Contains("N/A") ? "N/A" : "";
                if (dto.sampleDescription!.Contains("Woven") && !dto.Parameter!.Contains("N/A"))
                {
                    map["F7"] = (wp, dto, reportNo) => "30";
                    map["A5"] = (wp, dto, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Woven/Non-woven Fabric: method B---Loop trials Perimeter =200mm Speed =100mm/min"
                    : "Woven/Non-woven Fabric: method A---Stripe trials  Guage length=200mm  Speed =200mm/min.";
                }
                else if (dto.sampleDescription!.Contains("Knit") && !dto.Parameter!.Contains("N/A"))
                {
                    map["A5"] = (wp, dto, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Knitted Fabric: method B---Loop trials  Perimeter =200mm Speed =500mm/min" :
                    "Knitted Fabric: method A---Stripe trials Guage length=100mm Speed =500mm/min.";
                    map["F7"] = (wp, dto, reportNo) =>
                    dto.sampleDescription!.Contains("3") ? "3"
                    : dto.sampleDescription!.Contains("4") ? "4"
                    : dto.sampleDescription!.Contains("5") ? "5"
                    : dto.sampleDescription!.Contains("6") ? "6"
                    : dto.sampleDescription!.Contains("7") ? "7"
                    : dto.sampleDescription!.Contains("8") ? "8"
                    : dto.sampleDescription!.Contains("10") ? "10"
                    : "14";
                }
                map["L7"] = (wp, dto, reportNo) => "5";
                return map;
            },
            ["Water Resistance-Hydrostatic Pressure"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (wp, dto, reportNo) => reportNo,
                ["A3"] = (wp, dto, reportNo) => dto.Standard!,
                ["I7"] = (wp, dto, reportNo) => "2000",
                ["I15"] = (wp, dto, reportNo) => "2000",
            },
            ["Water Repellency-Spray Test"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (wp, dto, reportNo) => reportNo;
                map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                if (w.WashingProcedure!.Contains("Hand") && dto.Parameter!.Contains("1 Wash"))
                {
                    map["Y23"] = (w, dto, reportNo) => w.Temperature!;
                    map["AE23"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["A24"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                else if (dto.Parameter!.Contains("1 Wash"))
                {
                    map["A21"] = (w, dto, reportNo) => w.Bleach + " Cycle";
                    map["AE20"] = (w, dto, reportNo) => w.Temperature!;
                    map["N20"] = (w, dto, reportNo) => w.Program!;
                    map["M21"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["T20"] = (w, dto, reportNo) => w.DryCleanProcedure!;
                    map["V21"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ " : w.IronMethod!;
                    map["A24"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                else if (dto.Parameter!.Contains("Dry-clean"))
                {
                    map["L26"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                return map;
            },
            ["Seam Slippage"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => "ISO 13936-1:2004";
                map["A19"] = (w, dto, reportNo) => "ISO 13936-2:2004";
                if (dto.Parameter!.Contains("1 Wash"))
                {
                    map["G42"] = (w, dto, reportNo) => w.WashingProcedure!;
                    map["AJ42"] = (w, dto, reportNo) => w.Temperature!;
                    map["Q43"] = (w, dto, reportNo) => w.Ballast!;
                    map["L44"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["U44"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                    map["A45"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                    map["AE44"] = (w, dto, reportNo) => "1 Wash";
                }
                return map;
            },
            ["Attachment Strength"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (wp, dto, reportNo) => reportNo;
                if (dto.Standard!.Contains("EN 71") || dto.Standard!.Contains("16792"))
                {
                    map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                    map["A17"] = (wp, dto, reportNo) => dto.Standard!;
                }
                else
                {
                    map["A3"] = (wp, dto, reportNo) => "BS EN 17394-2:2020";
                    map["A17"] = (wp, dto, reportNo) => "CEN/TS 17394-3:2021";
                }
                return map;
            },
            ["Absorbency"] = (w, dto, reportNo) =>
            {
                var  map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (wp, dto, reportNo) => reportNo;
                map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                if (w.WashingProcedure!.Contains("Hand") && dto.Parameter!.Contains("1 Wash"))
                {
                    map["Y32"] = (w, dto, reportNo) => w.Temperature!;
                    map["AE32"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["A33"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                else if (dto.Parameter!.Contains("1 Wash"))
                {
                    map["A30"] = (w, dto, reportNo) => w.Bleach + " Cycle";
                    map["AE29"] = (w, dto, reportNo) => w.Temperature!;
                    map["N29"] = (w, dto, reportNo) => w.Program!;
                    map["M30"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["T29"] = (w, dto, reportNo) => w.DryCleanProcedure!;
                    map["V30"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ " : w.IronMethod!;
                    map["A33"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Bonding Strength"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (wp, dto, reportNo) => reportNo,
                ["A3"] = (wp, dto, reportNo) => dto.Standard!,
            },
            ["Electrostatic Properties"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (wp, dto, reportNo) => reportNo,
                ["A3"] = (wp, dto, reportNo) => dto.Standard!,
            },
        };



        private static readonly Dictionary<string, int> OffsetRule = new()
        {
            ["CF to Perspiration"] = 6,
            ["DS to Washing"] = 4,
            ["Dimensional Stability to Ironing"] = 3
        };
        private void WriteSamples(
            ExcelWorksheet ws,
            string[] slice,
            int[]? afmap,
            string[] cellAddrs,
            string[]? AfterWashCellAddrs,
            string itemName,
            string sampleDescription)
        {
            int offset = OffsetRule.GetValueOrDefault(itemName, 0);
            if (itemName == "DS to Washing" && !sampleDescription.Contains("Fabric")) offset = 0;
            if (afmap != null && afmap.Length > 0 && itemName == "DS to Washing" && !sampleDescription.Contains("Fabric") || itemName == "Appearance")
            {
                for (int i = 0; i < AfterWashCellAddrs!.Length; i++)
                {
                    ws.Cells[AfterWashCellAddrs![i]].Value = afmap[0];
                }
            }
            else if (afmap != null && afmap.Length > 0) 
            {
                for (int i = 0; i < afmap.Length; i++)
                {
                    ws.Cells[AfterWashCellAddrs![i]].Value = afmap[i];
                }
            }


            if (itemName == "Appearance")
            {
                for (int i = 0; i < cellAddrs.Length; i++)
                {
                    ws.Cells[cellAddrs[i]].Value = slice[0];
                }
            }
            else
            {
                for (int i = 0; i < slice.Length; i++)
                {
                    // 写入样本数据到指定的单元格地址
                    ws.Cells[cellAddrs[i]].Value = slice[i];

                    // 如果有偏移量，并且偏移后的单元格地址在范围内
                    if (offset > 0 && i + offset < cellAddrs.Length)
                    {
                        ws.Cells[cellAddrs[i + offset]].Value = slice[i];
                    }
                }
            }


        }

    }
}