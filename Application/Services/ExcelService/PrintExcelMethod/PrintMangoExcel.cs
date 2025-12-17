using OfficeOpenXml;
using static NX_lims_Softlines_Command_System.Application.Services.Factory.PrintExcelStrategyFactory;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.Helper;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelPrintTool;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.PrintExcelMethod
{

    public sealed class PrintMangoExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintMangoExcel(LabDbContextSec db)
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
            // 1) 模板 sheet
            var tplName = new TemplateSelector(TemplateSheetNames, TemplateSheetNamesNormal).GetTemplateName(itemName, dto.sampleDescription!);
            var template = pkg.Workbook.Worksheets[tplName];

            // 2) 计算需要几张 sheet
            var cellAddrs = CellMapper[itemName](itemName, dto.sampleDescription!,dto.MenuName!);
            string[]? AfterWashCellAddrs = null;
            if (itemName == "DS to Washing")
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName, dto.sampleDescription!);
            }



            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            int[]? afterWashMap = null;
            if (itemName == "DS to Washing" )
            {
                var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                if (wp == null) wp = new WetParameterIso();
                string? afterWash = wp!.AfterWash;
                string? iron = wp!.Iron;
                samples = SampleNumCounter.GetSample(dto.Sample!, afterWash, iron);
                afterWashMap = SampleNumCounter.ExpandWashNumbers(samples!, afterWash!,iron);
            }
            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->


            int offset = OffsetRule.GetValueOrDefault(itemName, 0); // 获取偏移量，默认为0
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            if (itemName == "DS to Washing" && dto.sampleDescription!.Contains("Garment")) capacity = 1;
            int sheetCnt = (int)Math.Ceiling(samples!.Length / (double)capacity);

            for (int idx = 0; idx < sheetCnt; idx++)
            {
                ExcelWorksheet ws;
                if (idx == 0 && samples.Length <= capacity)
                    ws = template;                                  // 用模板
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

                /* 计算当前 sheet 要写的样本区间 */
                int start = idx * capacity;                         // 本 sheet 起始样本索引
                int end = Math.Min(start + capacity, samples.Length);
                int count = end - start;                            // 本 sheet 要写的样本数量

                /* 取本 sheet 对应的那段样本 */
                string[] slice = samples.Skip(start).Take(count).ToArray();
                int[]? afmap = null;
                if (afterWashMap != null) afmap = afterWashMap.Skip(start).Take(count).ToArray();
                /* 把这段样本写进去 */
                WriteSamples(ws, slice, afmap, cellAddrs, AfterWashCellAddrs, itemName,dto.sampleDescription!);

                // 5) 其余参数
                if (dto.Type == "Wet")
                {
                    var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                    var extraMap = WetExtraMap.GetValueOrDefault(itemName, (w,dto, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, string, string>>())(wp!,dto, reportNo);

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
                    var extraMap = PhyExtraMap.GetValueOrDefault(itemName, (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>())(dto, reportNo);
                    foreach (var kv in extraMap)
                    {
                        ws.Cells[kv.Key].Value = kv.Value(dto, reportNo);
                    }
                }
            }
        }

        // 模板 sheet 名
        private static readonly Dictionary<string, string> TemplateSheetNamesNormal = new()
        {
            ["DS to Dry-clean"] = "DStoDryClean",
            ["CF to Washing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Rubbing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Light"] = "CFtoWashing&Rubbing&Light",
            ["CF to Perspiration"] = "CFtoPerspiration&Water&Dryclean",
            ["CF to Water"] = "CFtoPerspiration&Water&Dryclean",
            ["CF to Dry-clean"] = "CFtoPerspiration&Water&Dryclean",
            ["Weight"] = "Weight",
            ["Yarn Count"] = "Yarn Count",
            ["Pilling Resistance"] = "Pilling Resistance",
            ["Seam Slippage"] = "Seam Slippage",
            ["Tear Strength"] = "Tear Strength",
            ["Abrasion Resistance"] = "Abrasion&Snagging Resistance",
            ["Snagging Resistance"] = "Abrasion&Snagging Resistance",
        };
        private static readonly Dictionary<string, Dictionary<string, string>> TemplateSheetNames = new()
        {
            ["DS to Washing"] = new Dictionary<string, string>
            {
                {"Fabric", "DStoWashing-F" },
                {"Garment", "DStoWashing-G" },
                {"Socks", "DStoWashing-Acc" },
                {"Gloves", "DStoWashing-Acc" },
                {"Cap", "DStoWashing-Acc" },
            },
        };
        // 取映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string, string[]>> CellMapper = new()
        {
            ["DS to Washing"] = (n, m, y) => ExcelMangoMapper.GetFixedCellAddresses(m),
            ["DS to Dry-clean"] = (n, m, y) => ExcelMangoMapper.GetDStodrycleanCellAddresses(),
            ["CF to Washing"] = (n, m, y) => ExcelMangoMapper.GetCRLCellAddresses(n),
            ["CF to Rubbing"] = (n, m, y) => ExcelMangoMapper.GetCRLCellAddresses(n),
            ["CF to Light"] = (n, m, y) => ExcelMangoMapper.GetCRLCellAddresses(n),
            ["CF to Perspiration"] = (n, m, y) => ExcelMangoMapper.GetPWDCellAddresses(n),
            ["CF to Water"] = (n, m, y) => ExcelMangoMapper.GetPWDCellAddresses(n),
            ["CF to Dry-clean"] = (n, m, y) => ExcelMangoMapper.GetPWDCellAddresses(n),
            ["Weight"] = (n, m, y) => ExcelMangoMapper.GetWeightCellAddresses(),
            ["Yarn Count"] = (n, m, y) => ExcelMangoMapper.GetYarnCountCellAddresses(),
            ["Pilling Resistance"] = (n, m, y) => ExcelMangoMapper.GetPillingCellAddresses(y),
            ["Seam Slippage"] = (n, m, y) => ExcelMangoMapper.GetSTCellAddresses(n),
            ["Tear Strength"] = (n, m, y) => ExcelMangoMapper.GetSTCellAddresses(n),
            ["Abrasion Resistance"] = (n, m, y) => ExcelMangoMapper.GetASCellAddresses(n),
            ["Snagging Resistance"] = (n, m, y) => ExcelMangoMapper.GetASCellAddresses(n),
        };
        //取洗涤遍数映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> AfterWashCellMapper = new()
        {
            ["DS to Washing"] = (_, m) => ExcelMangoMapper.DStoWashingAf(m),
            ["DS to Dry-clean"] = (_, _) => ExcelMangoMapper.DStoDCAf(),
        };



        // 其余Wet固定/动态参数  →  (单元格, 取值Func)  
        private static readonly Dictionary<string, Func<WetParameterIso,CheckListDto, string, Dictionary<string, Func<WetParameterIso,CheckListDto, string, string>>>> WetExtraMap = new()
        {
            ["DS to Washing"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["BC1"] = (w, dto, reportNo) => reportNo;
                    map["AR3"] = (w, dto, reportNo) => (dto.Standard ?? "").Replace(",", " / ").TrimEnd(' ', '/');
                    map["AX4"] = (w, dto, reportNo) => w.WashingProcedure!;
                    map["BY4"] = (w, dto, reportNo) => w.Temperature!;
                    map["BG5"] = (w, dto, reportNo) => w.Ballast!;
                    map["BI6"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["AR7"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                else if (dto.sampleDescription!.Contains("Garment")) 
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["A3"] = (w, dto, reportNo) => (dto.Standard ?? "").Replace(",", " / ").TrimEnd(' ', '/');
                    map["I4"] = (w, dto, reportNo) => w.WashingProcedure!;
                    map["AK4"] = (w, dto, reportNo) => w.Temperature!;
                    map["T5"] = (w, dto, reportNo) => w.Ballast!;
                    map["V6"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["A7"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["DS to Dry-clean"] = (w, dto, reportNo) =>new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => (dto.Standard ?? "").Replace(",", " / ").TrimEnd(' ', '/'),
                ["AW4"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal"
            },
            ["CF to Washing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => "(ISO 105 C06:2010)",
                ["B4"] = (w, dto, reportNo) => w.Program!,
                ["E4"] = (w, dto, reportNo) => w.Temperature!,
                ["J5"] = (w, dto, reportNo) => w.SteelBallNum.ToString()!,
            },
            ["CF to Rubbing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A20"] = (w, dto, reportNo) => "(ISO 105-X12:2016)"
            },
            ["CF to Light"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A28"] = (w, dto, reportNo) => "(ISO 105 B02:2014)",
                ["B30"] = (w, dto, reportNo) => "L-5"
            },
            ["CF to Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => "(ISO 105 E04:2013)",
            },
            ["CF to Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A25"] = (w, dto, reportNo) => "(ISO 105 E01:2013)",
            },
            ["CF to Dry-clean"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A37"] = (w, dto, reportNo) => "(ISO 105 D01:2010)"
            },
        };
        // 其余Physics固定/动态参数  →  (单元格, 取值Func)  V
        private static readonly Dictionary<string, Func<CheckListDto, string, Dictionary<string, Func<CheckListDto, string, string>>>> PhyExtraMap = new()
        {
            ["Weight"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["J1"] = (dto, reportNo) => reportNo,
                ["A3"] = (dto, reportNo) => dto.Standard!
            },
            ["Pilling Resistance"] = (dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<CheckListDto, string, string>>();
                switch (dto.MenuName)
                {
                    case "Knit(Mango)":
                        map["M1"] = (dto, reportNo) => reportNo;
                        map["F3"] = (dto, reportNo) => dto.Standard!;
                        map["D4"] = (dto, reportNo) => dto.Parameter!;
                        break;
                    case "Woven(Mango)":
                        map["M1"] = (dto, reportNo) => reportNo;
                        map["F13"] = (dto, reportNo) => dto.Standard!;
                        map["D14"] = (dto, reportNo) => dto.Parameter!;
                        break;
                    default:
                        break;
                }
                return map;
            },
            ["Yarn Count"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {

                ["M1"] = (dto, reportNo) => reportNo,
                ["A3"] = (dto, reportNo) => dto.Standard!,
            },
            ["Seam Slippage"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["A3"] = (dto, reportNo) => dto.Standard!
            },
            ["Tear Strength"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["F3"] = (dto, reportNo) => dto.Standard!
            },
            ["Abrasion Resistance"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["A3"] = (dto, reportNo) => "ISO 12947-2:2016",
                ["C5"] = (dto, reportNo) => "9kPa",
                ["I5"] = (dto, reportNo) => "15000r",
                ["C11"] = (dto, reportNo) => "/"
            },
            ["Snagging Resistance"]
            = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["J24"] = (dto, reportNo) => "ASTM D3939/D3939M-13(2017)",
                ["C26"] = (dto, reportNo) => "600"
            }
        };

        //登记偏移量
        private static readonly Dictionary<string, int> OffsetRule = new()
        {
            ["DS to Washing"] = 4,
            ["DS to Dry-clean"] = 4,
            ["CF to Perspiration"] = 6,
            // 其余不写就代表单写
        };


        private void WriteSamples(
            ExcelWorksheet ws,
            string[] slice,
            int[]? afmap,
            string[] cellAddrs,
            string[]? AfterWashCellAddrs,
            string itemName,
            string SampleDescription)
        {
            int offset = OffsetRule.GetValueOrDefault(itemName, 0);
            if (itemName == "DS to Washing" && SampleDescription.Contains("Garment"))offset = 0;
            if (afmap != null && afmap.Length > 0 && (itemName == "DS to Washing" && SampleDescription.Contains("Garment")))
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

            if (itemName == "DS to Washing" && SampleDescription.Contains("Garment"))
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