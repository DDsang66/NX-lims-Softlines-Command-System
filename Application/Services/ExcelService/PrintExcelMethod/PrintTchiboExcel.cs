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

    public sealed class PrintTchiboExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintTchiboExcel(LabDbContextSec db)
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
                    FillSheet(pkg, dto.ItemName!, dto,Dto, reportNumber);
            }
            PackageWet.Save();
            PackagePhy.Save();

        }

        private void FillSheet(
            ExcelPackage pkg,
            string itemName,
            CheckListDto dto,
            ExcelSubmitDto esDto,
            string reportNo)
        {
            var tplName = new TemplateSelector(TemplateSheetNames, TemplateSheetNamesNormal).GetTemplateName(itemName, dto.sampleDescription!);
            var template = pkg.Workbook.Worksheets[tplName];
            // 2) 计算需要几张 sheet
            var cellAddrs = CellMapper[itemName](itemName, dto.sampleDescription!);
            string[]? AfterWashCellAddrs = null;
            if (itemName == "DS to Washing" || itemName == "DS to Dry-clean" 
                || itemName == "Appearance" || itemName == "Spirality/Skewing" || (itemName== "Water Repellency-Spray Test"&& dto.sampleDescription!.Contains("After Wash")))
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName, dto.sampleDescription!);
            }


            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            int[]? afterWashMap = null;
            if (itemName == "DS to Washing" || itemName == "DS to Dry-clean" 
                || itemName == "Appearance" || itemName == "Spirality/Skewing" || (itemName == "Water Repellency-Spray Test" && dto.sampleDescription!.Contains("After Wash")))
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
            if (itemName == "CF to Hot Pressing") { capacity = 3; }// 特例处理，实际容量为3
            if (itemName == "Appearance") { capacity = 1; }
            if (itemName == "DS to Washing"&&dto.sampleDescription!.Contains("Garment")) { capacity = 1; }
            if (itemName == "Absorbency") { capacity = 6; }
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
                WriteSamples(ws, slice, afmap, cellAddrs, AfterWashCellAddrs, itemName,dto.sampleDescription);
                //这里是分割样本的逻辑<-------------------------------------------------------------------------------------->
                // 5) 其余参数
                if (dto.Type == "Wet")
                {
                    var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                    var extraMap = WetExtraMap.GetValueOrDefault(itemName, (wp, dto,reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>())(wp!, dto, reportNo);

                    foreach (var kv in extraMap)
                    {
                        // 如果 wp 为 null，提供一个默认值或者跳过某些操作
                        if (wp == null)
                        {
                            var defaultWp = new WetParameterIso();
                            ws.Cells[kv.Key].Value = kv.Value(defaultWp, dto,reportNo);
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
                    var extraMap = PhyExtraMap.GetValueOrDefault(itemName, (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>())(wp!, dto, esDto, ws, reportNo);
                    foreach (var kv in extraMap)
                    {
                        // 如果 wp 为 null，提供一个默认值或者跳过某些操作
                        if (wp == null)
                        {
                            var defaultWp = new WetParameterIso();
                            ws.Cells[kv.Key].Value = kv.Value(defaultWp, dto, esDto, ws, reportNo);
                        }
                        else
                        {
                            ws.Cells[kv.Key].Value = kv.Value(wp, dto, esDto, ws, reportNo);
                        }
                    }
                }
            }


        }
        private static readonly Dictionary<string, string> TemplateSheetNamesNormal = new()
        {
            ["Weight"] = "Weight",
            ["Yarn Count"] = "Yarn Count",
            ["Pilling Resistance"] = "Pilling Resistance",
            ["Zipper Strength"] = "Zipper Strength-EN 16732",
            ["Resistance to Unsnapping of Snap Fasteners"] = "Resistance to Unsnapping",
            ["Water Resistance-Hydrostatic Pressure"] = "Hydroatatic Test",
            ["Water Repellency-Spray Test"] = "Water Repellency",
            ["Extension and Recovery"] = "Stretch&Recovery of Elastic",
            ["Air Permeability"] = "Air Permeability",
            ["Absorbency"] = "Absorbency",
            ["Attachment Strength"]= "Attachment Strength",
            ["Density"] = "Density",
            ["Appearance"] = "AppearanceAfterWashing",
            ["CF to Washing"] = "CFtoWRLS",
            ["CF to Rubbing"] = "CFtoWRLS",
            ["CF to Light"] = "CFtoWRLS",
            ["CF to Sea Water"] = "CFtoWRLS",
            ["CF to Perspiration"] = "CFtoPerspiration&Water",
            ["CF to Water"] = "CFtoPerspiration&Water",
            ["CF to Saliva"] = "CFtoSaliva&Sweat",
            ["CF to Sweat"] = "CFtoSaliva&Sweat",
            ["CF to Sublimation in Storage"] = "CFtoSHCl",
            ["CF to Hot Pressing"] = "CFtoSHCl",
            ["CF to Chlorinated Water"] = "CFtoSHCl",
        };
        private static readonly Dictionary<string, Dictionary<string[], string>> TemplateSheetNames = new()
        {
            ["DS to Washing"] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "DStoWashing-F" },
                {new[] { "Garment" },"DStoWashing-G"},
            },
            ["Seam Slippage"] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "Seam Slippage" },
                {new[] { "Garment" },"Seam Slippage-G"},
            },
        };
        private static readonly Dictionary<string, Func<string, string, string[]>> CellMapper = new()
        {
            ["Appearance"] = (_, _) => ExcelTchiboMapper.MapAppearance(),
            ["Weight"] = (_, _) => ExcelTchiboMapper.MapWeight(),
            ["Yarn Count"] = (_, _) => ExcelTchiboMapper.MapYarnCount(),
            ["Pilling Resistance"] = (_, m) => ExcelTchiboMapper.MapPilling(m),
            ["Zipper Strength"] = (_, _) => ExcelTchiboMapper.MapZipperStrength(),
            ["Resistance to Unsnapping of Snap Fasteners"] = (_, _) => ExcelTchiboMapper.MapUnsnapping(),
            ["Water Resistance-Hydrostatic Pressure"] = (_, _) => ExcelTchiboMapper.MapHydrostaticPressing(),
            ["Water Repellency-Spray Test"] = (_, m) => ExcelTchiboMapper.MapRepellency(m),
            ["Extension and Recovery"] = (_, _) => ExcelTchiboMapper.MapExtensionAndRecovery(),
            ["Air Permeability"] = (_, _) => ExcelTchiboMapper.MapAirPermeability(),
            ["Absorbency"] = (_, _) => ExcelTchiboMapper.MapAbsorbency(),
            ["Attachment Strength"] = (_, _) => ExcelTchiboMapper.MapAttachmentStrength(),
            ["Density"] = (_, _) => ExcelTchiboMapper.MapDensity(),
            ["CF to Washing"] = (_, _) => ExcelTchiboMapper.MapCFtoWashing(),
            ["CF to Rubbing"] = (_, _) => ExcelTchiboMapper.MapCFtoRubbing(),
            ["CF to Light"] = (_, _) => ExcelTchiboMapper.MapCFtoLight(),
            ["CF to Sea Water"] = (_, _) => ExcelTchiboMapper.MapCFtoSeaWater(),
            ["CF to Perspiration"] = (_, _) => ExcelTchiboMapper.MapCFtoPerspiration(),
            ["CF to Water"] = (_, _) => ExcelTchiboMapper.MapCFtoWater(),
            ["CF to Saliva"] = (_, _) => ExcelTchiboMapper.MapCFtoSalivaSweat(),
            ["CF to Sweat"] = (_, _) => ExcelTchiboMapper.MapCFtoSalivaSweat(),
            ["CF to Sublimation in Storage"] = (_, _) => ExcelTchiboMapper.MapCFtoSublimation(),
            ["CF to Hot Pressing"] = (_, _) => ExcelTchiboMapper.MapCFtoHotPressing(),
            ["CF to Chlorinated Water"] = (_, _) => ExcelTchiboMapper.MapCFtoCl(),
            ["DS to Washing"] = (_, m) => ExcelTchiboMapper.MapDStoWashing(m),
            ["Seam Slippage"] = (_, m) => ExcelTchiboMapper.MapSeamSlippage(m)
        };
        //取洗涤遍数映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> AfterWashCellMapper = new()
        {
            ["DS to Washing"] = (_, m) => ExcelTchiboMapper.DStoWashingAf(m),
            ["Appearance"] = (_, _) => ExcelTchiboMapper.AppearanceAf(),
            ["Water Repellency-Spray Test"] = (_, _) => ExcelTchiboMapper.SprayAf(),
        };
        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, string, Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>>> WetExtraMap = new()
        {

            ["DS to Washing"] = (w, dto, reportNo) =>
               {
                   var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                   if (dto.sampleDescription!.Contains("Fabric"))
                   {
                       map["BC1"] = (w, dto, reportNo) => reportNo;
                       map["AR3"] = (w, dto, reportNo) => dto.Standard!;
                       map["BA4"] = (w, dto, reportNo) => w.Temperature!;
                       map["BH4"] = (w, dto, reportNo) => w.Detergent!;
                       map["AV5"] = (w, dto, reportNo) => w.WashingProcedure!;
                       map["BP5"] = (w, dto, reportNo) => w.DryProcedure!;
                       map["AR6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                       map["AU7"] = (w, dto, reportNo) => w.Program!;
                   }
                   else if (dto.sampleDescription!.Contains("Garment"))
                   {
                       map["P1"] = (w, dto, reportNo) => reportNo;
                       map["A3"] = (w, dto, reportNo) => dto.Standard!;
                       map["L4"] = (w, dto, reportNo) => w.Temperature!;
                       map["S4"] = (w, dto, reportNo) => w.Detergent!;
                       map["A5"] = (w, dto, reportNo) => w.WashingProcedure!;
                       map["Y5"] = (w, dto, reportNo) => w.DryProcedure!;
                       map["A6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                       map["D7"] = (w, dto, reportNo) => w.Program!;
                   }
                   return map;
               },
            ["Appearance"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR4"] = (w, dto, reportNo) => dto.Standard!,
                ["BI13"] = (w, dto, reportNo) => w.IronMethod!,
                ["BA37"] = (w, dto, reportNo) => w.Temperature!,
                ["BH37"] = (w, dto, reportNo) => w.Detergent!,
                ["AV38"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["BT38"] = (w, dto, reportNo) => w.DryProcedure!,
                ["AR39"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!,
                ["BX39"] = (w, dto, reportNo) => w.Program!,
            },
            ["CF to Sublimation in Storage"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["H1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["D4"] = (w, dto, reportNo) => w.Temperature!,
                ["G4"] = (w, dto, reportNo) => "80",
                ["D7"] = (w, dto, reportNo) => w.Ballast!
            },
            ["CF to Hot Pressing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["H1"] = (w, dto, reportNo) => reportNo,
                ["A12"] = (w, dto, reportNo) => dto.Standard!,
                ["G13"] = (w, dto, reportNo) => w.Temperature!,
                ["A14"] = (w, dto, reportNo) => w.Iron=="L-5"?"该项目号最高可给5级":null!,
                ["R13"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod)==true ? "N/A" : "",
            },
            ["CF to Chlorinated Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["H1"] = (w, dto, reportNo) => reportNo,
                ["A27"] = (w, dto, reportNo) => dto.Standard!
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
                ["A20"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Light"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A28"] = (w, dto, reportNo) => dto.Standard!,
                ["B31"] = (w, dto, reportNo) => dto.Parameter!,
            },
            ["CF to Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A27"] = (w, dto, reportNo) => dto.Standard!,
                ["G26"] = (w, dto, reportNo) => dto.Parameter=="L-5" ? "该项目号最高可给5级" : null!,
            },
            ["CF to Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Saliva"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["H15"] = (w, dto, reportNo) => dto.Parameter == "L-5" ? "该项目号最高可给5级" : null!,
                ["G3"] = (w, dto, reportNo) => "√"
            },
            ["CF to Sweat"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["H15"] = (w, dto, reportNo) => dto.Parameter == "L-5" ? "该项目号最高可给5级" : null!,
                ["J3"] = (w, dto, reportNo) => "√"
            },
            ["CF to Sea Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A36"] = (w, dto, reportNo) => dto.Standard!,
            },
        };
        private static readonly Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>>> PhyExtraMap = new()
        {
            ["Weight"]= (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["J1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Yarn Count"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Pilling Resistance"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                if (dto.Standard!.Contains("12945-2"))
                {
                    map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                    map["F13"] = (wp, dto, esDto, ws, reportNo) => "DIN EN ISO 12945-2:2021,";
                    map["D14"] = (wp, dto, esDto, ws, reportNo) => "2000 revs"!;
                }
                else if (dto.Standard!.Contains("12945-1"))
                {
                    map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                    map["F3"] = (wp, dto, esDto, ws, reportNo) => "DIN EN ISO 12945-1:2021";
                }
                return map;
            },
            ["Zipper Strength"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Resistance to Unsnapping of Snap Fasteners"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Extension and Recovery"] = (wp, dto, esDto, ws, reportNo) => 
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                map["AC7"] = (wp, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("N/A")?"N/A":"";
                if (dto.sampleDescription!.Contains("Woven") && !dto.Parameter!.Contains("N/A"))
                {
                    map["F7"] = (wp, dto, esDto, ws, reportNo) => "30";
                    map["A5"] = (wp, dto, esDto, ws, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Woven/Non-woven Fabric: method B---Loop trials Perimeter =200mm Speed =100mm/min" 
                    : "Woven/Non-woven Fabric: method A---Stripe trials  Guage length=200mm  Speed =200mm/min.";
                }
                else if (dto.sampleDescription!.Contains("Knit") && !dto.Parameter!.Contains("N/A")) 
                {
                    map["A5"] = (wp, dto, esDto, ws, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Knitted Fabric: method B---Loop trials  Perimeter =200mm Speed =500mm/min" :
                    "Knitted Fabric: method A---Stripe trials Guage length=100mm Speed =500mm/min.";
                    map["F7"] = (wp, dto, esDto, ws, reportNo) =>
                    dto.sampleDescription!.Contains("3")?"3"
                    : dto.sampleDescription!.Contains("4") ? "4"
                    : dto.sampleDescription!.Contains("5") ? "5"
                    : dto.sampleDescription!.Contains("6") ? "6"
                    : dto.sampleDescription!.Contains("7") ? "7"
                    : dto.sampleDescription!.Contains("8") ? "8"
                    : dto.sampleDescription!.Contains("10") ? "10"
                    :"14";
                }
                map["L7"] = (wp, dto, esDto, ws, reportNo) => "5";
                return map;
            },
            ["Water Resistance-Hydrostatic Pressure"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Water Repellency-Spray Test"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                if (dto.sampleDescription!.Contains("After Wash"))
                {
                    map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                    map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                    map["K20"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                    map["R20"] = (wp, dto, esDto, ws, reportNo) => wp.Detergent!;
                    map["L21"] = (wp, dto, esDto, ws, reportNo) => wp.WashingProcedure!;
                    map["A22"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                    map["N22"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                    map["A23"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                }
                else
                {
                    map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                    map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                    map["K20"] = (wp, dto, esDto, ws, reportNo) => "-";
                    map["R20"] = (wp, dto, esDto, ws, reportNo) => "-";
                    map["L21"] = (wp, dto, esDto, ws, reportNo) => "-";
                    map["A22"] = (wp, dto, esDto, ws, reportNo) => "-";
                    map["N22"] = (wp, dto, esDto, ws, reportNo) => "-";
                    map["A23"] = (wp, dto, esDto, ws, reportNo) => "-";
                }
                return map;
            },
            ["Seam Slippage"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => esDto.ReportNumber!;
                if (dto.sampleDescription!.Contains("Fabric")) map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["J3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                    var sample = ws.Cells["D3"].Value?.ToString();

                    var cellOrder = new List<string> { "A5", "A6", "A7", "A8", "A9", "A10", "A11", "A12", "A13", "A14", "A15", "A16" };
                    var reasonCellOrder = cellOrder.Select(c => "Y" + c.Substring(1)).ToList();
                    if (sample.ToLower().Contains("shell"))
                    {
                        reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                    }
                    if (sample.ToLower().Contains("lining"))
                    {
                        reasonCellOrder = cellOrder.Select(c => "Y" + c.Substring(1)).ToList();
                    }
                    var descMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Side"] = "Side Seam",
                        ["Sleeve"] = "Sleeve Seam",
                        ["Armhole"] = "Armhole Seam",
                        ["Shoulder"] = "Shoulder Seam",
                        ["Armprit"] = "Armprit Seam",
                        ["Front Panel"] = "Front Panel Seam",
                        ["Back Panel"] = "Back Panel Seam",
                        ["OutSide"] = "Out-Side Seam",
                        ["InSide"] = "In-Side Seam",
                        ["Back Rise"] = "Back Rise Seam",
                        ["Front Crotch"] = "Front Crotch Seam",
                        ["Cross"] = "Cross Seam",
                    };

                    var seamInfos = esDto.SeamParameter
                                      ?.FirstOrDefault(s => s.Sample == sample)   // 找到当前行样本
                                      ?.LocationInfos
                                      ?.Where(x => !string.IsNullOrWhiteSpace(x.Location)) // 去掉空Location
                                      .ToList();
                    if (seamInfos?.Count > 0)
                    {
                        for (int i = 0; i < seamInfos.Count && i < cellOrder.Count; i++)
                        {
                            string location = seamInfos[i].Location!.Trim();
                            if (descMap.TryGetValue(location, out var desc))
                            {
                                string cell = cellOrder[i];
                                map[cell] = (wp, dto, esDto, ws, reportNo) => desc;   // 填入对应描述
                            }
                        }
                    }

                    for (int i = 0; i < seamInfos!.Count && i < cellOrder.Count; i++)
                    {
                        var info = seamInfos[i];
                        string location = info.Location!.Trim();

                        // 1. 填描述（原逻辑）
                        if (descMap.TryGetValue(location, out var desc))
                        {
                            string cell = cellOrder[i];
                            map[cell] = (wp, dto, esDto, ws, reportNo) => desc;
                        }

                        // 2. 当 IsNA == false 时，把 Reason 写到同行 J 列
                        if (info.IsNA == true && !string.IsNullOrWhiteSpace(info.Reason))
                        {
                            string reasonCell = reasonCellOrder[i];
                            string reason = "N/A；" + info.Reason;         // 捕获局部变量
                            map[reasonCell] = (wp, dto, esDto, ws, reportNo) => reason;
                        }
                    }
                }
                return map;
            },
            ["Air Permeability"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["F5"] = (wp, dto, esDto, ws, reportNo) => "100",
                ["E6"] = (wp, dto, esDto, ws, reportNo) => "20",
            },
            ["Absorbency"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["A30"] = (wp, dto, esDto, ws, reportNo) => "ISO 5077:2007 / ISO 3759:2011 / ISO 6330:2021",
                ["J31"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!,
                ["Q31"] = (wp, dto, esDto, ws, reportNo) => wp.Detergent!,
                ["E32"] = (wp, dto, esDto, ws, reportNo) => wp.WashingProcedure!,
                ["AC32"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!,
                ["A33"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!,
                ["Z33"] = (wp, dto, esDto, ws, reportNo) => wp.Program!
            },
            ["Attachment Strength"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["A20"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Density"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
        };



        private static readonly Dictionary<string, int> OffsetRule = new()
        {
            ["CF to Perspiration"] = 6,
            ["DS to Washing"] = 4,
            ["Water Repellency-Spray Test"] = 3,
            ["Absorbency"]= 6,
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
            if (itemName== "Water Repellency-Spray Test"&& !sampleDescription.Contains("Before and After Wash")) offset = 0;
            if (afmap != null && afmap.Length > 0 && AfterWashCellAddrs != null && AfterWashCellAddrs.Length > 0 && itemName == "Appearance")
            {
                for (int i = 0; i < AfterWashCellAddrs.Length; i++)
                {
                    ws.Cells[AfterWashCellAddrs![i]].Value = afmap[0];
                }
            }
            else if (afmap != null && afmap.Length > 0 && itemName == "DS to Washing" && sampleDescription.Contains("Garment"))
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
            else if (itemName == "CF to Hot Pressing") 
            {
                for (int i = 0; i < slice.Length; i++)
                {
                    ws.Cells[cellAddrs[i]].Value = slice[i];
                    ws.Cells[cellAddrs[i+3]].Value = slice[i];
                    ws.Cells[cellAddrs[i + 6]].Value = slice[i];
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