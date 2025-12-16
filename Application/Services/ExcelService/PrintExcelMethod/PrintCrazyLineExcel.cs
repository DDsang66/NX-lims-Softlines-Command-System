using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelPrintTool;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.Helper;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using OfficeOpenXml;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.PrintExcelMethod
{
    public class PrintCrazyLineExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintCrazyLineExcel(LabDbContextSec db)
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
            foreach (var row in selectedRows!)
            {
                checkLists.Add(new CheckListDto
                {
                    ItemName = row.itemName,
                    Standard = row.standards,
                    Parameter = row.parameters,
                    Type = row.types,
                    Sample = row.samples,
                    MenuName = menu,
                    sampleDescription = sampleDescription,
                });
            }
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
            var cellAddrs = CellMapper[itemName](itemName, dto.MenuName!);
            string[]? AfterWashCellAddrs = null;
            if (itemName == "DS to Washing" || itemName == "DS to Dry-clean" || itemName == "Appearance" || itemName == "Spirality/Skewing")
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName, dto.MenuName!);
            }



            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            int[]? afterWashMap = null;
            if (itemName == "DS to Washing" || itemName == "DS to Dry-clean" || itemName == "Appearance" || itemName == "Spirality/Skewing")
            {
                var wp = _db.WetParameterAatccs
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                if (wp == null) wp = new WetParameterAatcc();
                string? afterWash = wp!.AfterWash;
                string? iron = wp!.Iron;
                samples = SampleNumCounter.GetSample(dto.Sample!, afterWash, iron);
                afterWashMap = SampleNumCounter.ExpandWashNumbers(samples!, afterWash!, iron);
            }
            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            int offset = 0;
            if (dto.sampleDescription!.Contains("Fabric"))
            {
                offset = OffsetRule.GetValueOrDefault(itemName, 0);
            }// 获取偏移量，默认为0
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
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
                /* 把这段样本写进去 */
                WriteSamples(ws, slice, afmap, cellAddrs, AfterWashCellAddrs, itemName,dto.sampleDescription);

                // 5) 其余参数
                if (dto.Type == "Wet")
                {
                    var wp = _db.WetParameterAatccs
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                    var extraMap = WetExtraMap.GetValueOrDefault(itemName, (wp, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>())(wp, dto, reportNo);

                    foreach (var kv in extraMap)
                    {
                        // 如果 wp 为 null，提供一个默认值或者跳过某些操作
                        if (wp == null)
                        {
                            var defaultWp = new WetParameterAatcc();
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
                    var extraMap = PhyExtraMap.GetValueOrDefault(itemName, (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>())(dto, reportNo);
                    foreach (var kv in extraMap)
                    {
                        ws.Cells[kv.Key].Value = kv.Value(dto, reportNo);
                    }
                }
            }
        }
        private static readonly Dictionary<string, string> TemplateSheetNamesNormal = new()
        {
            ["CF to Washing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Rubbing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Light"] = "CFtoWashing&Rubbing&Light",
            ["CF to Perspiration"] = "CFtoPerspiration&Water&Dryclean",
            ["CF to Water"] = "CFtoPerspiration&Water&Dryclean",
            ["CF to Dry-clean"] = "CFtoPerspiration&Water&Dryclean",
            ["Weight"] = "Weight",
            ["Seam Slippage"] = "Seam Slippage",
            ["Pilling Resistance"] = "Pilling&Snagging",
            ["Snagging Resistance"] = "Pilling&Snagging",
            ["Spirality/Skewing"] = "Spirality",
            ["Small Parts"] = "Small Part",
            ["Resistance to Snapping of Snap Fasteners"] = "Snapping & Unsnapping",
            ["Resistance to Unsnapping of Snap Fasteners"] = "Snapping & Unsnapping",

        };
        private static readonly Dictionary<string, Dictionary<string, string>> TemplateSheetNames = new()
        {
            ["DS to Washing"] = new Dictionary<string, string>
            {
                {"Fabric", "DStoWashing-F" },
                {"Garment","DStoWashing-G"},
                {"Socks","DStoWashing-Acc"},
                {"Gloves","DStoWashing-Acc"},
                {"Cap","DStoWashing-Acc"},
            },
            ["DS to Dry-clean"] = new Dictionary<string, string>
            {
                {"Fabric", "DStoDryclean-F" },
                {"Garment", "DStoDryclean-G" },
            },
            ["Spirality/Skewing"] = new Dictionary<string, string>
            {
                {"Fabric", "Spirality" },
                {"Garment","Spirality"}
            },
            ["Zipper Strength"] = new Dictionary<string, string>
            {
                 {"EN","Zipper Strength-ASTM D2061"},
                 {"ASTM","Zipper Strength-EN 16732"}
            }
        };


        // 取映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> CellMapper = new()
        {
            ["DS to Washing"] = (_, m) => ExcelCrazyLineMapper.MapDStoWasing(m),
            ["DS to Dry-clean"] = (_, m) => ExcelCrazyLineMapper.MapDStoDC(m),
            ["CF to Washing"] = (n, _) => ExcelCrazyLineMapper.MapWRL(n),
            ["CF to Rubbing"] = (n, _) => ExcelCrazyLineMapper.MapWRL(n),
            ["CF to Light"] = (n, _) => ExcelCrazyLineMapper.MapWRL(n),
            ["CF to Perspiration"] = (n, _) => ExcelCrazyLineMapper.MapPWD(n),
            ["CF to Water"] = (n, _) => ExcelCrazyLineMapper.MapPWD(n),
            ["CF to Dry-clean"] = (n, _) => ExcelCrazyLineMapper.MapPWD(n),
            ["Spirality/Skewing"] = (_, m) => ExcelCrazyLineMapper.MapSpirality(m),
            ["Weight"] = (_, _) => ExcelCrazyLineMapper.MapWeight(),
            ["Pilling Resistance"] = (n, _) => ExcelCrazyLineMapper.MapPS(n),
            ["Seam Slippage"] = (_, _) => ExcelCrazyLineMapper.MapSeamSlippage(),
            ["Snagging Resistance"] = (n, _) => ExcelCrazyLineMapper.MapPS(n),
            ["Zipper Strength"] = (_, _) => ExcelCrazyLineMapper.MapRegular(),
            ["Resistance to Snapping of Snap Fasteners"] = (_, _) => ExcelCrazyLineMapper.MapRegular(),
            ["Resistance to Unsnapping of Snap Fasteners"] = (_, _) => ExcelCrazyLineMapper.MapRegular(),
            ["Small Parts"] = (_, _) => ExcelCrazyLineMapper.MapRegular(),
        };

        //取洗涤遍数映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> AfterWashCellMapper = new()
        {
            ["DS to Washing"] = (_, m) => ExcelCrazyLineMapper.DStoWashingAf(m),
            ["DS to Dry-clean"] = (_, m) => ExcelCrazyLineMapper.DStoDCAf(m),
            ["Spirality/Skewing"] = (_, _) => ExcelCrazyLineMapper.SpiralityAf(),
        };


        private static readonly Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>>> WetExtraMap = new()
        {
            ["DS to Washing"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>();
                if (w.WashingProcedure!.Contains("Machine"))
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["A5"] = (w, dto, reportNo) => w.Cycle + " Cycle";
                    map["V4"] = (w, dto, reportNo) => w.Temperature!;
                    map["E4"] = (w, dto, reportNo) => w.Program!;
                    map["M5"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["K4"] = (w, dto, reportNo) => w.DryCondition!;
                    map["A8"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;

                }
                else if (w.WashingProcedure.Contains("Hand"))
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["H7"] = (w, dto, reportNo) => w.Temperature!;
                    map["M7"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["A8"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["A3"] = (w, dto, reportNo) => "AATCC TM 135-2018t";
                    map["V5"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["A3"] = (w, dto, reportNo) =>"AATCC TM 150-2018t/AATCC TS006";
                    map["V5"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                }
                return map;
            },
            ["DS to Dry-clean"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>();
                if (dto.sampleDescription?.Contains("Fabric") == true)
                {
                    map["M1"] = (w, dto, reportNo) => reportNo;
                    map["A3"] = (w, dto, reportNo) => "AATCC TM158-1978e10(2016)e";
                    map["F4"] = (w, dto, reportNo) => w.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                else
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["A3"] = (w, dto, reportNo) => "AATCC TM158-1978e10(2016)e";
                    map["G4"] = (w, dto, reportNo) => w.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                return map;
            },
            ["CF to Washing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["E1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => "AATCC TM61-2013e(2020)e2",
                ["B4"] = (w, dto, reportNo) => w!.Program!,
                ["F4"] = (w, dto, reportNo) => w!.Temperature!,
                ["H5"] = (w, dto, reportNo) => w!.SteelBallNum.ToString()!,
                ["J5"] = (w, dto, reportNo) => w!.SteelBallType!,
                ["I4"] = (w, dto, reportNo) => w!.Detergent!,
            },
            ["CF to Rubbing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["E1"] = (w, dto, reportNo) => reportNo,
                ["A20"] = (w, dto, reportNo) => "AATCC TM8-2016e(2022)e"

            },
            ["CF to Light"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["E1"] = (w, dto, reportNo) => reportNo,
                ["A28"] = (w, dto, reportNo) => "AATCC TM16.3-2020",
                ["B32"] = (w, dto, reportNo) => "20"
            },
            ["CF to Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => "AATCC TM15-2021e"

            },
            ["CF to Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A25"] = (w, dto, reportNo) => "AATCC TM107-2022e",
            },
            ["CF to Dry-clean"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A37"] = (w, dto, reportNo) => "AATCC TM132-2004e3(2013)e3",
            },
            ["Spirality/Skewing"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>();
                map["P1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.sampleDescription!.Contains("Garment") == true ? "AATCC TM 179-2023, Method 2, Option 3" : "AATCC TM 179-2023, Method 1, Option 1";
                if (w.WashingProcedure!.Contains("Machine"))
                {
                    map["O31"] = (w, dto, reportNo) => "AATCC TM 179-2023";
                    map["D32"] = (w, dto, reportNo) => w.Program!;
                    map["I32"] = (w, dto, reportNo) => w.DryCondition!;
                    map["U32"] = (w, dto, reportNo) => w.Temperature!;
                    map["A33"] = (w, dto, reportNo) => w.Cycle!;
                    map["M33"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["V33"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                }
                else if (w.WashingProcedure.Contains("Hand"))
                {
                    map["O35"] = (w, dto, reportNo) => "AATCC TM 179-2023";
                    map["G36"] = (w, dto, reportNo) => w.Temperature!;
                    map["K36"] = (w, dto, reportNo) => w.DryProcedure!;
                }
                return map;
            },
        };

        private static readonly Dictionary<string, Func<CheckListDto, string, Dictionary<string, Func<CheckListDto, string, string>>>> PhyExtraMap = new()
        {
            ["Weight"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["J1"] = (dto, reportNo) => reportNo,
                ["A3"] = (dto, reportNo) => dto.Standard!
            },
            ["Pilling Resistance"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["H3"] = (dto, reportNo) => dto.Standard!,
                ["D4"] = (dto, reportNo) => "30"
            },
            ["Seam Slippage"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["A3"] = (dto, reportNo) => dto.Standard!
            },
            ["Snagging Resistance"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["J15"] = (dto, reportNo) => dto.Standard!,
                ["C17"] = (dto, reportNo) => "600"
            },
            ["Small Parts"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo
            },
            ["Zipper Strength"] = (dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<CheckListDto, string, string>>();
                map["M1"] = (dto, reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("EN"))
                {
                    map["A3"] = (dto, reportNo) => "EN 16732:2025";
                }
                else
                {
                    map["A3"] = (dto, reportNo) => "ASTM D2061-07(2021)";
                }
                return map;
            },
            ["Resistance to Snapping of Snap Fasteners"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["A3"] = (dto, reportNo) => dto.Standard!
            },
            ["Resistance to Unsnapping of Snap Fasteners"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["A26"] = (dto, reportNo) => dto.Standard!
            },
        };

        //登记偏移量
        private static readonly Dictionary<string, int> OffsetRule = new()
        {
            ["DS to Washing"] = 4,
            ["DS to Dry-clean"] = 4,
            // 其余不写就代表单写
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
            int offset = 0;
            if (sampleDescription.Contains("Fabric"))
            {
                offset = OffsetRule.GetValueOrDefault(itemName, 0);
            }


            if (afmap != null && afmap.Length > 0 && (itemName == "DS to Washing" || itemName == "DS to Dry-clean") && sampleDescription.Contains("Garment"))
            {
                for (int i = 0; i < afmap.Length; i++)
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
