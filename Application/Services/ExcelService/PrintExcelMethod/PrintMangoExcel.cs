using NX_lims_Softlines_Command_System.Application.DTO;
using OfficeOpenXml;
using static NX_lims_Softlines_Command_System.Tools.Factory.PrintExcelStrategyFactory;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;

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
            #region
            //string ordernumber = Dto.ReportNumber!;
            //string buyer = Dto.Buyer!;
            //string menu = Dto.MenuName!;
            //var selectedRows = Dto.SelectedRows;

            //List<CheckListDto> checkLists = new List<CheckListDto>();
            //foreach (var row in selectedRows!)
            //{
            //    checkLists.Add(new CheckListDto
            //    {
            //        ItemName = row.itemName,
            //        Standard = row.standards,
            //        Parameter = row.parameters,
            //        Type = row.types,
            //        Sample = row.samples,
            //    });
            //}

            //foreach (var Item in checkLists)
            //{
            //    if (Item.ItemName != null)
            //    {

            //        switch (Item.Type)
            //        {
            //            case "Wet":
            //                #region WET
            //                if (Item.ItemName == "DS to Washing")
            //                {
            //                    var DW = _db!.WetParameters.FirstOrDefault(p => p.Item == Item.ItemName && p.OrderNumber == ordernumber);
            //                    var worksheet_0 = PackageWet.Workbook.Worksheets[0];
            //                    worksheet_0.Cells["BC1"].Value = ordernumber;

            //                    string formattedStandard = (Item.Standard ?? "").Replace(",", " / ").TrimEnd(' ', '/');
            //                    string[] samples = Item.Sample!.Split(',');
            //                    var cellAddresses = ExcelMangoMapper.GetFixedCellAddresses();
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_0.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                        worksheet_0.Cells[cellAddresses[i + 4]].Value = samples[i].Trim();
            //                    }

            //                    worksheet_0.Cells["AR3"].Value = formattedStandard;
            //                    worksheet_0.Cells["AX4"].Value = DW!.WashingProcedure;
            //                    worksheet_0.Cells["BY4"].Value = DW.Temperature;
            //                    worksheet_0.Cells["BG5"].Value = DW.Ballast;
            //                    worksheet_0.Cells["BI6"].Value = DW.DryProcedure;
            //                    worksheet_0.Cells["AR11"].Value = DW.SCI ?? null;

            //                }
            //                else if (Item.ItemName == "DS to Dry-clean")
            //                {
            //                    var DC = _db!.WetParameters.FirstOrDefault(p => p.Item == Item.ItemName && p.OrderNumber == ordernumber);
            //                    var worksheet_1 = PackageWet.Workbook.Worksheets[1];
            //                    worksheet_1.Cells["BC1"].Value = ordernumber;
            //                    string formattedStandard = (Item.Standard ?? "").Replace(",", " / ").TrimEnd(' ', '/');
            //                    worksheet_1.Cells["AR3"].Value = formattedStandard;
            //                    worksheet_1.Cells["AW4"].Value = DC!.IsSensitive == "Y" ? "Sensitive" : "Normal";

            //                    string[] samples = Item.Sample!.Split(',');
            //                    var cellAddresses = ExcelMangoMapper.GetDStodrycleanCellAddresses();
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_1.Cells[cellAddresses[i]].Value = samples[i].Trim();
            //                        worksheet_1.Cells[cellAddresses[i + 4]].Value = samples[i].Trim();// 假设从 G1 开始存放样本值
            //                    }

            //                }
            //                else if (Item.ItemName == "CF to Washing" || Item.ItemName == "CF to Rubbing" || Item.ItemName == "CF to Light")
            //                {
            //                    var CF = _db!.WetParameters.FirstOrDefault(p => p.Item == Item.ItemName && p.OrderNumber == ordernumber);
            //                    var worksheet_2 = PackageWet.Workbook.Worksheets[2];
            //                    worksheet_2.Cells["D1"].Value = ordernumber;
            //                    if (Item.ItemName == "CF to Washing")
            //                    {
            //                        worksheet_2.Cells["A3"].Value = "(ISO 105 C06:2010)";
            //                        worksheet_2.Cells["B4"].Value = CF!.Program;
            //                        worksheet_2.Cells["E4"].Value = CF.Temperature;
            //                        worksheet_2.Cells["J5"].Value = CF.SteelBall;
            //                    }
            //                    else if (Item.ItemName == "CF to Rubbing")
            //                    {
            //                        worksheet_2.Cells["A20"].Value = "(ISO 105-X12:2016)";
            //                    }
            //                    else if (Item.ItemName == "CF to Light")
            //                    {
            //                        worksheet_2.Cells["A28"].Value = "(ISO 105 B02:2014)";
            //                        worksheet_2.Cells["B30"].Value = "L-5";
            //                    }

            //                    string[] samples = Item.Sample!.Split(',');
            //                    var cellAddresses = ExcelMangoMapper.GetCRLCellAddresses(Item.ItemName);
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_2.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                    }

            //                }
            //                else if (Item.ItemName == "CF to Perspiration" || Item.ItemName == "CF to Water" || Item.ItemName == "CF to Dry-clean")
            //                {
            //                    var CF = _db!.WetParameters.FirstOrDefault(p => p.Item == Item.ItemName && p.OrderNumber == ordernumber);
            //                    var worksheet_3 = PackageWet.Workbook.Worksheets[3];
            //                    worksheet_3.Cells["D1"].Value = ordernumber;
            //                    if (Item.ItemName == "CF to Dry-clean")
            //                    {
            //                        worksheet_3.Cells["A37"].Value = "(ISO 105 D01:2010)";
            //                    }
            //                    else if (Item.ItemName == "CF to Perspiration")
            //                    {
            //                        worksheet_3.Cells["A3"].Value = "(ISO 105 E04:2013)";
            //                        string[] samples1 = Item.Sample!.Split(',');
            //                        var cellAddresses1 = ExcelMangoMapper.GetPWDCellAddresses(Item.ItemName);
            //                        for (int i = 0; i < samples1.Length; i++)
            //                        {
            //                            worksheet_3.Cells[cellAddresses1[i + 6]].Value = samples1[i].Trim(); // 假设从 G1 开始存放样本值
            //                        }
            //                    }
            //                    else if (Item.ItemName == "CF to Water")
            //                    {
            //                        worksheet_3.Cells["A25"].Value = "(ISO 105 E01:2013)";
            //                    }
            //                    string[] samples = Item.Sample!.Split(',');
            //                    var cellAddresses = ExcelMangoMapper.GetPWDCellAddresses(Item.ItemName);
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_3.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                    }


            //                }
            //                else
            //                {
            //                    continue;
            //                }

            //                #endregion
            //                break;
            //            case "Physics":

            //                if (Item.ItemName == "Weight")
            //                {
            //                    var worksheet_0 = PackagePhy.Workbook.Worksheets[0];
            //                    worksheet_0.Cells["J1"].Value = ordernumber;
            //                    worksheet_0.Cells["A3"].Value = Item.Standard;
            //                    string[] samples = Item.Sample!.Split(',');
            //                    var cellAddresses = ExcelMangoMapper.GetWeightCellAddresses();
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_0.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                    }
            //                }
            //                else if (Item.ItemName == "Yarn Count")
            //                {
            //                    var worksheet_1 = PackagePhy.Workbook.Worksheets[1];
            //                    worksheet_1.Cells["M1"].Value = ordernumber;
            //                    worksheet_1.Cells["A3"].Value = Item.Standard;
            //                    worksheet_1.Cells["D10"].Value = Item.Sample;

            //                }
            //                else if (Item.ItemName.Contains("Pilling"))
            //                {
            //                    var worksheet_2 = PackagePhy.Workbook.Worksheets[2];
            //                    worksheet_2.Cells["M1"].Value = ordernumber;
            //                    switch (menu)
            //                    {
            //                        case "Knit(Mango)":
            //                            worksheet_2.Cells["F3"].Value = Item.Standard;
            //                            worksheet_2.Cells["D4"].Value = Item.Parameter;
            //                            break;
            //                        case "Woven(Mango)":
            //                            worksheet_2.Cells["F13"].Value = Item.Standard;
            //                            worksheet_2.Cells["D14"].Value = Item.Parameter;
            //                            break;
            //                        default:
            //                            break;
            //                    }
            //                    string[] samples = Item.Sample!.Split(',');
            //                    var cellAddresses = ExcelMangoMapper.GetPillingCellAddresses(menu);
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_2.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                    }
            //                }
            //                else if (Item.ItemName == "Seam Slippage" || Item.ItemName.Contains("Tear"))
            //                {

            //                    switch (Item.ItemName)
            //                    {
            //                        case "Seam Slippage":
            //                            var worksheet_3 = PackagePhy.Workbook.Worksheets[3];
            //                            worksheet_3.Cells["M1"].Value = ordernumber;
            //                            worksheet_3.Cells["A3"].Value = Item.Standard;
            //                            string[] samples = Item.Sample!.Split(',');
            //                            var cellAddresses = ExcelMangoMapper.GetSTCellAddresses(Item.ItemName);
            //                            for (int i = 0; i < samples.Length; i++)
            //                            {
            //                                worksheet_3.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                            }
            //                            break;
            //                        case "Tear Strength":
            //                            var worksheet_4 = PackagePhy.Workbook.Worksheets[4];
            //                            worksheet_4.Cells["M1"].Value = ordernumber;
            //                            worksheet_4.Cells["F3"].Value = Item.Standard;
            //                            string[] samples_1 = Item.Sample!.Split(',');
            //                            var cellAddresses_1 = ExcelMangoMapper.GetSTCellAddresses(Item.ItemName);
            //                            for (int i = 0; i < samples_1.Length; i++)
            //                            {
            //                                worksheet_4.Cells[cellAddresses_1[i]].Value = samples_1[i].Trim(); // 假设从 G1 开始存放样本值
            //                            }
            //                            break;
            //                        default:
            //                            break;
            //                    }
            //                }
            //                else if (Item.ItemName == "Abraison Resistance" || Item.ItemName == "Snagging Resistance")
            //                {
            //                    var worksheet_5 = PackagePhy.Workbook.Worksheets[5];
            //                    worksheet_5.Cells["M1"].Value = ordernumber;
            //                    switch (Item.ItemName)
            //                    {
            //                        case "Abraison Resistance":
            //                            worksheet_5.Cells["F3"].Value = "ISO 12947-2:2016";
            //                            worksheet_5.Cells["C5"].Value = "9kPa";
            //                            worksheet_5.Cells["I5"].Value = "15000r";
            //                            worksheet_5.Cells["C11"].Value = "/";
            //                            break;
            //                        case "Snagging Resistance":
            //                            worksheet_5.Cells["J24"].Value = "ASTM D3939/D3939M-13(2017)";
            //                            worksheet_5.Cells["C26"].Value = "600";
            //                            break;
            //                    }

            //                    string[] samples = Item.Sample!.Split(',');
            //                    var cellAddresses = ExcelMangoMapper.GetSTCellAddresses(Item.ItemName);
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_5.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                    }
            //                }
            //                else
            //                {
            //                    continue;
            //                }
            //                break;
            //            default:
            //                break;

            //        }

            //    }

            //}


            //PackageWet.Save();
            //PackagePhy.Save();
            #endregion
            string reportNumber = Dto.ReportNumber!;
            string buyer = Dto.Buyer!;
            string menu = Dto.MenuName!;
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
                    MenuName = menu
                });
            }
            foreach (var dto in checkLists)
            {
                Console.WriteLine($"{dto.ItemName} -> {dto.Type}");
                var pkg = dto.Type == "Wet" ? PackageWet : PackagePhy;
                if (TemplateSheetNames.ContainsKey(dto.ItemName!))
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
            var tplName = TemplateSheetNames[itemName];
            var template = pkg.Workbook.Worksheets[tplName];

            // 2) 计算需要几张 sheet
            var cellAddrs = CellMapper[itemName](itemName, dto.MenuName!);
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            int offset = OffsetRule.GetValueOrDefault(itemName, 0); // 获取偏移量，默认为0
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            int sheetCnt = (int)Math.Ceiling(samples.Length / (double)capacity);

            for (int idx = 0; idx < sheetCnt; idx++)
            {
                ExcelWorksheet ws;
                if (idx == 0 && samples.Length <= capacity)
                    ws = template;                                  // 用模板
                else
                {
                    string newSheetName = $"{tplName} ({idx + 1})";
                    ws = pkg.Workbook.Worksheets.Copy(tplName, newSheetName);
                }

                /* 计算当前 sheet 要写的样本区间 */
                int start = idx * capacity;                         // 本 sheet 起始样本索引
                int end = Math.Min(start + capacity, samples.Length);
                int count = end - start;                            // 本 sheet 要写的样本数量

                /* 取本 sheet 对应的那段样本 */
                string[] slice = samples.Skip(start).Take(count).ToArray();

                /* 把这段样本写进去 */
                WriteSamples(ws, slice, cellAddrs, itemName);

                // 5) 其余参数
                if (dto.Type == "Wet")
                {
                    var wp = _db.WetParameterISO
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                    var extraMap = WetExtraMap.GetValueOrDefault(itemName, new());

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
        private static readonly Dictionary<string, string> TemplateSheetNames = new()
        {
            ["DS to Washing"] = "DStoWashing",
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

        // 取映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> CellMapper = new()
        {
            ["DS to Washing"] = (_, _) => ExcelMangoMapper.GetFixedCellAddresses(),
            ["DS to Dry-clean"] = (_, _) => ExcelMangoMapper.GetDStodrycleanCellAddresses(),
            ["CF to Washing"] = (n, _) => ExcelMangoMapper.GetCRLCellAddresses(n),
            ["CF to Rubbing"] = (n, _) => ExcelMangoMapper.GetCRLCellAddresses(n),
            ["CF to Light"] = (n, _) => ExcelMangoMapper.GetCRLCellAddresses(n),
            ["CF to Perspiration"] = (n, _) => ExcelMangoMapper.GetPWDCellAddresses(n),
            ["CF to Water"] = (n, _) => ExcelMangoMapper.GetPWDCellAddresses(n),
            ["CF to Dry-clean"] = (n, _) => ExcelMangoMapper.GetPWDCellAddresses(n),
            ["Weight"] = (_, _) => ExcelMangoMapper.GetWeightCellAddresses(),
            ["Yarn Count"] = (_, _) => ExcelMangoMapper.GetYarnCountCellAddresses(),
            ["Pilling Resistance"] = (_, m) => ExcelMangoMapper.GetPillingCellAddresses(m),
            ["Seam Slippage"] = (n, _) => ExcelMangoMapper.GetSTCellAddresses(n),
            ["Tear Strength"] = (n, _) => ExcelMangoMapper.GetSTCellAddresses(n),
            ["Abrasion Resistance"] = (n, _) => ExcelMangoMapper.GetASCellAddresses(n),
            ["Snagging Resistance"] = (n, _) => ExcelMangoMapper.GetASCellAddresses(n),
        };

        // 其余Wet固定/动态参数  →  (单元格, 取值Func)  
        private static readonly Dictionary<string, Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>> WetExtraMap = new()
        {
            ["DS to Washing"] = new()
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => (dto.Standard ?? "").Replace(",", " / ").TrimEnd(' ', '/'),
                ["AX4"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["BY4"] = (w, dto, reportNo) => w.Temperature!,
                ["BG5"] = (w, dto, reportNo) => w.Ballast!,
                ["BI6"] = (w, dto, reportNo) => w.DryProcedure!,
                ["AR11"] = (w, dto, reportNo) => w.SpecialCareInstruction ?? null
            },
            ["DS to Dry-clean"] = new()
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => (dto.Standard ?? "").Replace(",", " / ").TrimEnd(' ', '/'),
                ["AW4"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal"
            },
            ["CF to Washing"] = new()
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => "(ISO 105 C06:2010)",
                ["B4"] = (w, dto, reportNo) => w.Program!,
                ["E4"] = (w, dto, reportNo) => w.Temperature!,
                ["J5"] = (w, dto, reportNo) => w.SteelBallNum.ToString()!,
            },
            ["CF to Rubbing"] = new()
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A20"] = (w, dto, reportNo) => "(ISO 105-X12:2016)"
            },
            ["CF to Light"] = new()
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A28"] = (w, dto, reportNo) => "(ISO 105 B02:2014)",
                ["B30"] = (w, dto, reportNo) => "L-5"
            },
            ["CF to Perspiration"] = new()
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => "(ISO 105 E04:2013)",
            },
            ["CF to Water"] = new()
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A25"] = (w, dto, reportNo) => "(ISO 105 E01:2013)",
            },
            ["CF to Dry-clean"] = new()
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
            string[] cellAddrs,
            string itemName)
        {
            int offset = OffsetRule.GetValueOrDefault(itemName, 0);
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