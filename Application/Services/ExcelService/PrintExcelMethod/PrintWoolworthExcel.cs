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

    public sealed class PrintWoolworthExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintWoolworthExcel(LabDbContextSec db)
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
            if (itemName == "DS to Washing" || itemName == "Appearance")
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName, dto.sampleDescription!);
            }


            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            int[]? afterWashMap = null;
            if (itemName == "DS to Washing" || itemName == "Appearance")
            {
                var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                if (wp == null) wp = new WetParameterIso();
                string? afterWash = wp!.AfterWash;
                afterWash = afterWash = string.Join(", ", dto.Sample!
                        .Split(',')
                        .Select(s => s.Trim())
                        .SelectMany(s => new[] { $"{s}-1 Wash" }));
                string? iron = wp!.Iron;
                string? ironMethod = wp!.IronMethod;
                samples = SampleNumCounter.GetSample(dto.Sample!, afterWash, iron, ironMethod);
                afterWashMap = SampleNumCounter.ExpandWashNumbers(samples!, afterWash!,iron);
            }
            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            int offset = 0; // 假设没有偏移
            offset = OffsetRule.GetValueOrDefault(itemName, 0);
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            if (itemName == "Appearance") { capacity = 1; }
            if (itemName == "DS to Washing"&& !dto.sampleDescription!.Contains("Fabric")) { capacity = 1; }
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
                    var extraMap = WetExtraMap.GetValueOrDefault(itemName, (wp, dto, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto,string, string>>())(wp!, dto, reportNo);

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
                    var extraMap = PhyExtraMap.GetValueOrDefault(itemName, (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>())(wp,dto, esDto, ws, reportNo);
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
            ["Piece Weight"] = "Piece Weight",
            ["Yarn Count"] = "Yarn Count",
            ["Pilling Resistance"] = "Pilling Resistance",
            ["Zipper Strength"] = "Zipper Strength-EN 16732",
            ["Water Resistance-Hydrostatic Pressure"] = "Hydroatatic Test",
            ["Air Permeability"] = "Air Permeability",
            ["Attachment Strength"]= "Attachment Strength",
            ["Tensile Strength"] = "Tensile Strength",
            ["Tear Strength"] = "Tearing Strength",
            ["Density"] = "Density",

            ["Appearance"] = "AppearanceAfterWashing",
            ["CF to Washing"] = "CFtoWRLS",
            ["CF to Rubbing"] = "CFtoWRLS",
            ["CF to Light"] = "CFtoWRLS",
            ["CF to Sea Water"] = "CFtoWRLS",
            ["CF to Perspiration"] = "CFtoPWCl",
            ["CF to Water"] = "CFtoPWCl",
            ["CF to Saliva"] = "CFtoSaliva&Sweat",
            ["CF to Sweat"] = "CFtoSaliva&Sweat",
            ["CF to Chlorinated Water"] = "CFtoPWCl",
            ["Spirality/Skewing"] = "Spirality",
            ["DS to Dry-clean"] = "DStoDryclean",
        };
        private static readonly Dictionary<string, Dictionary<string[], string>> TemplateSheetNames = new()
        {
            ["DS to Washing"] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "DStoWashing-F" },
                {new[] { "Garment" },"DStoWashing-G"},
                {new[] { "Socks" }, "DStoWashing-Acc" },
                {new[] { "Gloves" }, "DStoWashing-Acc" },
                {new[] { "Cap" }, "DStoWashing-Acc" },
            },
            ["Seam Slippage"] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "Seam Slippage&Strength" },
                {new[] { "Garment" },"Seam Slippage&Strength-G"},
            },
            ["Seam Strength"] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "Seam Slippage&Strength" },
                { new[] {"Knit" ,"Garment"},"Seam Bursting"},
                {new[] { "Garment" },"Seam Slippage&Strength-G"},
            },
        };
        private static readonly Dictionary<string, Func<string, string, string[]>> CellMapper = new()
        {
            ["Weight"] = (n, m) => ExcelWoolworthMapper.MapWeight(),
            ["Piece Weight"] = (n, m) => ExcelWoolworthMapper.MapWeight(),
            ["Yarn Count"] = (n, m) => ExcelWoolworthMapper.MapYarnCount(),
            ["Pilling Resistance"] = (n, m) => ExcelWoolworthMapper.MapPilling(),
            ["Zipper Strength"] = (n, m) => ExcelWoolworthMapper.MapZipperStrength(),
            ["Water Resistance-Hydrostatic Pressure"] = (n, m) => ExcelWoolworthMapper.MapHydrostaticPressing(),
            ["Air Permeability"] = (n, m) => ExcelWoolworthMapper.MapAirPermeability(),
            ["Attachment Strength"] = (n, m) => ExcelWoolworthMapper.MapAttachmentStrength(),
            ["Tensile Strength"] = (n, m) => ExcelWoolworthMapper.MapTensileStrength(),
            ["Tear Strength"] = (n, m) => ExcelWoolworthMapper.MapTearStrength(),
            ["Density"] = (n, m) => ExcelWoolworthMapper.MapDensity(),
            ["Seam Slippage"] = (n, m) => ExcelWoolworthMapper.MapSeamSlippage(m),
            ["Seam Strength"] = (n, m) => ExcelWoolworthMapper.MapSeamStrength(m),
            ["Bursting"] = (n, m) => ExcelWoolworthMapper.MapBurstingStrength(),

            ["CF to Washing"] = (n, m) => ExcelWoolworthMapper.MapCFtoWashing(),
            ["CF to Rubbing"] = (n, m) => ExcelWoolworthMapper.MapCFtoRubbing(),
            ["CF to Light"] = (n, m) => ExcelWoolworthMapper.MapCFtoLight(),
            ["CF to Sea Water"] = (n, m) => ExcelWoolworthMapper.MapCFtoSeaWater(),
            ["CF to Perspiration"] = (n, m) => ExcelWoolworthMapper.MapCFtoPerspiration(),
            ["CF to Water"] = (n, m) => ExcelWoolworthMapper.MapCFtoWater(),
            ["CF to Saliva"] = (n, m) => ExcelWoolworthMapper.MapCFtoSalivaSweat(),
            ["CF to Sweat"] = (n, m) => ExcelWoolworthMapper.MapCFtoSalivaSweat(),
            ["CF to Chlorinated Water"] = (n, m) => ExcelWoolworthMapper.MapCFtoCl(),
            ["DS to Washing"] = (n, m) => ExcelWoolworthMapper.MapDStoWashing(m),
            ["Appearance"] = (n, m) => ExcelWoolworthMapper.MapAppearance(),
            ["Spirality/Skewing"] = (n, m) => ExcelWoolworthMapper.MapSpirality(),
            ["DS to Dry-clean"] = (n, m) => ExcelWoolworthMapper.MapDStoDC()
        };
        //取洗涤遍数映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> AfterWashCellMapper = new()
        {
            ["DS to Washing"] = (_, m) => ExcelWoolworthMapper.DStoWashingAf(m),
            ["Appearance"] = (_, _) => ExcelWoolworthMapper.AppearanceAf(),
            ["Spirality/Skewing"] = (_, _) => ExcelWoolworthMapper.SpiralityAf(),
        };
        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, string, Dictionary<string, Func<WetParameterIso,CheckListDto,string, string>>>> WetExtraMap = new()
        {
            ["DS to Washing"] = (w, dto, reportNo) =>
               {
                   var map = new Dictionary<string, Func<WetParameterIso,CheckListDto,string, string>>();
                   if (dto.sampleDescription!.Contains("Fabric"))
                   {
                       map["BC1"] = (w, dto, reportNo) => reportNo;
                       map["AR3"] = (w, dto, reportNo) => dto.Standard!;
                       map["BA4"] = (w, dto, reportNo) => w.Temperature!;
                       map["BB5"] = (w, dto, reportNo) => w.WashingProcedure!;
                       map["AR6"] = (w, dto, reportNo) => w.DryProcedure!;
                       map["BI6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                       map["BC6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                   }
                   else if (dto.sampleDescription!.Contains("Garment"))
                   {
                       map["P1"] = (w, dto, reportNo) => reportNo;
                       map["A3"] = (w, dto, reportNo) => dto.Standard!;
                       map["L4"] = (w, dto, reportNo) => w.Temperature!;
                       map["J5"] = (w, dto, reportNo) => w.WashingProcedure!;
                       map["AG5"] = (w, dto, reportNo) => w.DryProcedure!;
                       map["N6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                       map["A6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                   }
                   else
                   {
                       map["N1"] = (w, dto, reportNo) => reportNo;
                       map["A3"] = (w, dto, reportNo) => dto.Standard!;
                       map["L4"] = (w, dto, reportNo) => w.Temperature!;
                       map["J5"] = (w, dto, reportNo) => w.WashingProcedure!;
                       map["AG5"] = (w, dto, reportNo) => w.DryProcedure!;
                       map["N6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                       map["A6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                   }
                   return map;
               },
            ["Spirality/Skewing"] = (w, dto, reportNo) => new Dictionary<string, Func< WetParameterIso,CheckListDto,string, string>>
            {
                ["P1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["L33"] = (w, dto, reportNo) => w.Temperature!,
                ["S33"] = (w, dto, reportNo) => w.Detergent!,
                ["Q34"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["A35"] = (w, dto, reportNo) => w.DryProcedure!,
                ["R35"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!,
                ["A36"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!,
                ["AD35"] = (w, dto, reportNo) => w.Program!,
            },
            ["DS to Dry-clean"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>> 
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => (dto.Standard ?? "").Replace(",", " / ").TrimEnd(' ', '/'),
                ["AW4"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal"
            },
            ["Appearance"] = (w, dto, reportNo) => new Dictionary<string, Func< WetParameterIso,CheckListDto,string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR4"] = (w, dto, reportNo) => dto.Standard!,
                ["BI13"] = (w, dto, reportNo) => w.IronMethod!,
                ["BA37"] = (w, dto, reportNo) => w.Temperature!,
                ["BH37"] = (w, dto, reportNo) => w.Detergent!,
                ["BE38"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["AU39"] = (w, dto, reportNo) => w.DryProcedure!,
                ["BF39"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!,
                ["AR40"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!,
                ["BL39"] = (w, dto, reportNo) => w.Program!,
            },
            ["CF to Chlorinated Water"] = (w, dto, reportNo) => new Dictionary<string, Func< WetParameterIso,CheckListDto,string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A40"] = (w, dto, reportNo) => dto.Standard!,
                ["E41"] = (w, dto, reportNo) => "50"
            },
            ["CF to Washing"]= (w, dto, reportNo) => new Dictionary<string, Func< WetParameterIso,CheckListDto,string, string>> 
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["B4"] = (w, dto, reportNo) => w.Program!,
                ["E4"] = (w, dto, reportNo) => w.Temperature!,
                ["L5"] = (w, dto, reportNo) => w.SteelBallNum!.ToString()!,
            },
            ["CF to Rubbing"] = (w, dto, reportNo) => new Dictionary<string, Func< WetParameterIso,CheckListDto,string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A20"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Light"] = (w, dto, reportNo) => new Dictionary<string, Func< WetParameterIso,CheckListDto,string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A28"] = (w, dto, reportNo) => dto.Standard!,
                ["B31"] = (w, dto, reportNo) => dto.Parameter!,
            },
            ["CF to Water"] = (w, dto, reportNo) => new Dictionary<string, Func< WetParameterIso,CheckListDto,string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A27"] = (w, dto, reportNo) => dto.Standard!,        
            },
            ["CF to Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func< WetParameterIso,CheckListDto,string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Saliva"] = (w, dto, reportNo) => new Dictionary<string, Func< WetParameterIso,CheckListDto,string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["G3"] = (w, dto, reportNo) => "√"
            },
            ["CF to Sweat"] = (w, dto, reportNo) => new Dictionary<string, Func< WetParameterIso,CheckListDto,string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["J3"] = (w, dto, reportNo) => "√"
            },
            ["CF to Sea Water"] = (w, dto, reportNo) => new Dictionary<string, Func< WetParameterIso,CheckListDto,string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A36"] = (w, dto, reportNo) => dto.Standard!,
            },
        };
        private static readonly Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>>> PhyExtraMap = new()
        {
            ["Weight"]= (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>
            {
                ["J1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Piece Weight"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>
            {
                ["J1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Yarn Count"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Pilling Resistance"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                map["F3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                map["D4"] = (wp, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("2000 revs")?"2000 revs"
                : dto.Parameter!.Contains("500 revs") ? "500 revs" 
                : "1000 revs";
                return map;
            },
            ["Zipper Strength"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Water Resistance-Hydrostatic Pressure"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["E9"] = (wp, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("3000 mmbar") ? "3000"
                : dto.Parameter!.Contains("1500 mmbar") ? "1500"
                : dto.Parameter!.Contains("800 mmbar") ? "800"
                : "0",
                ["L9"] = (wp, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("3000 mmbar") ? "3060"
                : dto.Parameter!.Contains("1500 mmbar") ? "1530"
                : dto.Parameter!.Contains("800 mmbar") ? "816"
                : "0",
                ["E17"] = (wp, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("3000 mmbar") ? "3000"
                : dto.Parameter!.Contains("1500 mmbar") ? "1500"
                : dto.Parameter!.Contains("800 mmbar") ? "800"
                : "0",
                ["L17"] = (wp, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("3000 mmbar") ? "3060"
                : dto.Parameter!.Contains("1500 mmbar") ? "1530"
                : dto.Parameter!.Contains("800 mmbar") ? "816"
                : "0",
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
                    var reasonCellOrder = new List<string>();
                    if (dto.Sample!.Contains("Shell") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Shell"))
                    {
                        map["Q4"] = (wp, dto, esDto, ws, reportNo) => "√";
                        reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                    }
                    else if (dto.Sample.Contains("Lining") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Lining"))
                    {
                        map["AF4"] = (wp, dto, esDto, ws, reportNo) => "√";
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
                            string reason = "N/A；" + info.Reason;          // 捕获局部变量
                            map[reasonCell] = (wp, dto, esDto, ws, reportNo) => reason;
                        }
                    }
                }
                return map;
            },
            ["Seam Strength"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("Garment") && dto.sampleDescription!.Contains("Knit"))
                {
                    map["J5"] = (wp, dto, esDto, ws, reportNo) => "ISO 13938-2:2019";
                    var sample = ws.Cells["D5"].Value?.ToString();

                    var cellOrder = new List<string> { "A7", "A8", "A9", "A10", "A11", "A12", "A13", "A14", "A15", "A16", "A17", "A18" };
                    var reasonCellOrder = new List<string>();
                    if (dto.Sample!.Contains("Shell") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Shell"))
                    {
                        map["Q6"] = (wp, dto, esDto, ws, reportNo) => "√";
                        reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                    }
                    else if (dto.Sample.Contains("Lining") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Lining"))
                    {
                        map["AF6"] = (wp, dto, esDto, ws, reportNo) => "√";
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
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["J18"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                    var sample = ws.Cells["D3"].Value?.ToString();
                    var cellOrder = new List<string> { "A5", "A6", "A7", "A8", "A9", "A10", "A11", "A12", "A13", "A14", "A15", "A16" };
                    var reasonCellOrder = new List<string>();
                    if (dto.Sample!.Contains("Shell") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Shell"))
                    {
                        map["Q4"] = (wp, dto, esDto, ws, reportNo) => "√";
                        reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                    }
                    else if (dto.Sample.Contains("Lining") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Lining"))
                    {
                        map["AF4"] = (wp, dto, esDto, ws, reportNo) => "√";
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
                            string reason = "N/A；" + info.Reason;          // 捕获局部变量
                            map[reasonCell] = (wp, dto, esDto, ws, reportNo) => reason;
                        }
                    }
                }
                return map;
            },
            ["Air Permeability"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["F5"] = (wp, dto, esDto, ws, reportNo) => "100",
                ["E6"] = (wp, dto, esDto, ws, reportNo) => "20",
            },
            ["Attachment Strength"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                if (dto.Standard!.Contains("EN 71") || dto.Standard!.Contains("16792"))
                {
                    map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                    map["A18"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                }
                else
                {
                    map["A3"] = (wp, dto, esDto, ws, reportNo) => "BS EN 17394-2:2020";
                    map["A18"] = (wp, dto, esDto, ws, reportNo) => "CEN/TS 17394-3:2021";
                }
                return map;
            },
            ["Tear Strength"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wwp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Tensile Strength"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Bursting Strength"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["I3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Density"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet,string, string>>
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
            ["DS to Dry-clean"] = 4,
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
            if (itemName=="DS to Washing" && !sampleDescription.Contains("Fabric") )offset = 0;
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