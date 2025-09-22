using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper;
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
            #region
            //string ordernumber = Dto.ReportNumber!;
            //string buyer = Dto.Buyer!;
            //string additionalRequired = Dto.AdditionalRequire!;
            //string sampleDescription = Dto.SampleDescription!;
            //var selectedRows = Dto.SelectedRows!;


            //List<CheckListDto> checkLists = new List<CheckListDto>();
            //foreach (var row in selectedRows)
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
            //                if (Item.ItemName == "DS to Washing")
            //                {

            //                    var dwContext = _db!.WetParameters.FirstOrDefault(p => p.Item == Item.ItemName && p.OrderNumber == ordernumber);
            //                    if (dwContext!.WashingProcedure.Contains("Machine"))
            //                    {
            //                        var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap" }
            //                        .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            //                        var worksheet_machine = matched switch
            //                        {
            //                            "Garment" => PackageWet.Workbook.Worksheets[6],
            //                            "Fabric" => PackageWet.Workbook.Worksheets[0],
            //                            "Socks" => PackageWet.Workbook.Worksheets[9],
            //                            "Gloves" => PackageWet.Workbook.Worksheets[9],
            //                            "Cap" => PackageWet.Workbook.Worksheets[9],
            //                            _ => PackageWet.Workbook.Worksheets[0]
            //                        };
            //                        worksheet_machine.Cells["P1"].Value = ordernumber;
            //                        worksheet_machine.Cells["A3"].Value = "AATCC TM 135-2018t";
            //                        worksheet_machine.Cells["A5"].Value = dwContext.Cycle + " Cycle";
            //                        worksheet_machine.Cells["V4"].Value = dwContext.Temperature;
            //                        worksheet_machine.Cells["E4"].Value = dwContext.Program;
            //                        worksheet_machine.Cells["M5"].Value = dwContext.DryProcedure;
            //                        worksheet_machine.Cells["K4"].Value = dwContext.DryCondition;
            //                        worksheet_machine.Cells["A6"].Value = dwContext.SCI ?? null;

            //                        string[] samples = Item.Sample!.Split(',');
            //                        var cellAddresses = ExcelCrazyLineMapper.MapDStoWasingMachine(sampleDescription!);
            //                        if (sampleDescription!.Contains("Fabric"))
            //                        {
            //                            for (int i = 0; i < samples.Length; i++)
            //                            {
            //                                worksheet_machine.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                                worksheet_machine.Cells[cellAddresses[i + 4]].Value = samples[i].Trim();
            //                            }

            //                        }
            //                        else 
            //                        {
            //                            for (int i = 0; i < samples.Length; i++)
            //                            {
            //                                worksheet_machine.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                            }
            //                        }
            //                    }
            //                    else if (dwContext!.WashingProcedure.Contains("Hand Wash"))
            //                    {
            //                        var mapSheet = new Dictionary<string[], int>
            //                        {
            //                            [new[] { "Garment" }] = 7,
            //                            [new[] { "Fabric" }] = 1,
            //                            [new[] { "Socks", "Gloves", "Cap" }] = 10
            //                        };
            //                        var sheetIdx = mapSheet.First(kv => kv.Key.Any(k => sampleDescription?.Contains(k) == true)).Value;
            //                        var ws = PackageWet.Workbook.Worksheets[sheetIdx];

            //                        // 行列映射表： cell  -> 值（动态列、标准号、公共值）
            //                        var mapData = new Dictionary<string, object>
            //                        {
            //                            ["G4"] = dwContext.Temperature,   // 公共列
            //                            ["K4"] = dwContext.DryProcedure,   // 公共列（Garment 特殊列在下面覆盖）
            //                            ["A6"] = dwContext.SCI ?? null
            //                        };

            //                        // 按类别覆盖差异值
            //                        if (sampleDescription?.Contains("Garment") == true)
            //                        {
            //                            mapData["M1"] = ordernumber;
            //                            mapData["A3"] = "AATCC TM 135-2018t/AATCC TS006";
            //                            mapData["J4"] = dwContext.DryProcedure; // 覆盖公共列
            //                        }
            //                        else if (sampleDescription?.Contains("Fabric") == true)
            //                        {
            //                            mapData["O1"] = ordernumber;
            //                            mapData["A3"] = "AATCC TM 135-2018t/AATCC TS006";
            //                        }
            //                        else // Socks/Glaves/Cap
            //                        {
            //                            mapData["P1"] = ordernumber;
            //                            mapData["A3"] = "AATCC TM 150-2018t/AATCC TS006";
            //                        }

            //                        // 4. 一次性写入
            //                        foreach (var kv in mapData)
            //                            ws.Cells[kv.Key].Value = kv.Value;
            //                        string[] samples = Item.Sample!.Split(',');
            //                        var cellAddresses = ExcelCrazyLineMapper.MapDStoWasingHand(sampleDescription!);
            //                        if (sampleDescription!.Contains("Fabric"))
            //                        {
            //                            for (int i = 0; i < samples.Length; i++)
            //                            {
            //                                ws.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                                ws.Cells[cellAddresses[i + 4]].Value = samples[i].Trim();
            //                            }

            //                        }
            //                        else
            //                        {
            //                            for (int i = 0; i < samples.Length; i++)
            //                            {
            //                                ws.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                            }
            //                        }
            //                    }
            //                    else { continue; }
            //                    continue;
            //                }
            //                else if (Item.ItemName == "CF to Washing" || Item.ItemName == "CF to Rubbing" || Item.ItemName == "CF to Light")
            //                {
            //                    var cfContext = _db!.WetParameters.FirstOrDefault(p => p.Item == Item.ItemName && p.OrderNumber == ordernumber);
            //                    var worksheet_3 = PackageWet.Workbook.Worksheets[3];
            //                    worksheet_3.Cells["E1"].Value = ordernumber;
            //                    switch (Item.ItemName)
            //                    {
            //                        case "CF to Washing":
            //                            worksheet_3.Cells["A3"].Value = "AATCC TM61-2013e(2020)e2";
            //                            worksheet_3.Cells["B4"].Value = cfContext!.Program;
            //                            worksheet_3.Cells["F4"].Value = cfContext!.Temperature;
            //                            worksheet_3.Cells["H5"].Value = cfContext!.SteelBall;
            //                            worksheet_3.Cells["J5"].Value = cfContext!.WashingProcedure.Contains("Hand Wash Cold") ? "Rubbow" : "Steel";
            //                            break;
            //                        case "CF to Rubbing":
            //                            worksheet_3.Cells["A20"].Value = "AATCC TM8-2016e(2022)e";
            //                            break;
            //                        case "CF to Light":
            //                            worksheet_3.Cells["A28"].Value = "AATCC TM16.3-2020";
            //                            worksheet_3.Cells["B32"].Value = "20";
            //                            break;
            //                    }
            //                    string[] samples = Item.Sample!.Split(',');
            //                    var cellAddresses = ExcelCrazyLineMapper.MapWRL(Item.ItemName);
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_3.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                    }
            //                }
            //                else if (Item.ItemName == "DS to Dry-clean")
            //                {
            //                    var dcContext = _db!.WetParameters.FirstOrDefault(p => p.Item == Item.ItemName && p.OrderNumber == ordernumber);
            //                    var mapSheet = new Dictionary<string[], int>
            //                    {
            //                        [new[] { "Garment" }] = 8,
            //                        [new[] { "Fabric" }] = 2,
            //                        [new[] { "Socks", "Gloves", "Cap" }] = 11
            //                    };
            //                    var sheetIdx = mapSheet.First(kv => kv.Key.Any(k => sampleDescription?.Contains(k) == true)).Value;
            //                    var worksheet_DC = PackageWet.Workbook.Worksheets[sheetIdx];
            //                    var mapData = new Dictionary<string, object>{};
            //                    if (sampleDescription?.Contains("Garment") == true)
            //                    {
            //                        mapData["P1"] = ordernumber;
            //                        mapData["A3"] = "AATCC TM158 - 1978e10(2016)e";
            //                        mapData["H4"] = dcContext!.IsSensitive == "Y" ? "Sensitive" : "Normal";
            //                    }
            //                    else if (sampleDescription?.Contains("Fabric") == true)
            //                    {
            //                        mapData["M1"] = ordernumber;
            //                        mapData["A3"] = "AATCC TM158 - 1978e10(2016)e";
            //                        mapData["F4"] = dcContext!.IsSensitive=="Y"?"Sensitive":"Normal";
            //                    }
            //                    else // Socks/Glaves/Cap
            //                    {
            //                        mapData["P1"] = ordernumber;
            //                        mapData["A3"] = "AATCC TM158 - 1978e10(2016)e";
            //                        mapData["H4"] = dcContext!.IsSensitive == "Y" ? "Sensitive" : "Normal";
            //                    }


            //                    foreach (var kv in mapData)
            //                        worksheet_DC.Cells[kv.Key].Value = kv.Value;
            //                    string[] samples = Item.Sample!.Split(',');
            //                    var cellAddresses = ExcelCrazyLineMapper.MapDStoDC(sampleDescription!);
            //                    if (sampleDescription!.Contains("Fabric"))
            //                    {
            //                        for (int i = 0; i < samples.Length; i++)
            //                        {
            //                            worksheet_DC.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                            worksheet_DC.Cells[cellAddresses[i + 4]].Value = samples[i].Trim();
            //                        }

            //                    }
            //                    else
            //                    {
            //                        for (int i = 0; i < samples.Length; i++)
            //                        {
            //                            worksheet_DC.Cells[cellAddresses[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                        }
            //                    }



            //                }
            //                else if (Item.ItemName == "CF to Perspiration" || Item.ItemName == "CF to Water" || Item.ItemName == "CF to Dry-clean") 
            //                {
            //                    var worksheet_4 = PackageWet.Workbook.Worksheets[4];
            //                    worksheet_4.Cells["D1"].Value = ordernumber;
            //                    _ = Item.ItemName switch
            //                    {
            //                        "CF to Perspiration" => worksheet_4.Cells["A3"].Value = "AATCC TM15-2021e",
            //                        "CF to Water" => worksheet_4.Cells["A25"].Value = "AATCC TM107-2022e",
            //                        "CF to Dry-clean" => worksheet_4.Cells["A37"].Value = "AATCC TM132-2004e3(2013)e3",
            //                        _ => null
            //                    };
            //                    string[] samples = Item.Sample!.Split(',');
            //                    var cellAddresses = ExcelCrazyLineMapper.MapPWD(Item.ItemName);
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_4.Cells[cellAddresses[i]].Value = samples[i].Trim();
            //                    }
            //                }
            //                    break;
            //            case "Physics":
            //                if (Item.ItemName == "Weight")
            //                {
            //                    var worksheet_0 = PackagePhy.Workbook.Worksheets[0];
            //                    worksheet_0.Cells["J1"].Value = ordernumber;
            //                    worksheet_0.Cells["A3"].Value = "ASTM D3776/D3776M-20 option C";
            //                    var MapWeight = ExcelCrazyLineMapper.MapWeight();
            //                    string[] samples = Item.Sample!.Split(',');
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_0.Cells[MapWeight[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                    }
            //                }
            //                else if (Item.ItemName == "Snagging Resistance" || Item.ItemName == "Pilling Resistance")
            //                {
            //                    var worksheet_1 = PackagePhy.Workbook.Worksheets[1];
            //                    worksheet_1.Cells["M1"].Value = ordernumber;
            //                    switch (Item.ItemName)
            //                    {
            //                        case "Snagging Resistance":
            //                            worksheet_1.Cells["J15"].Value = "ASTM D3939/D3939M-13(2017)";
            //                            worksheet_1.Cells["C17"].Value = "600";
            //                            break;
            //                        case "Pilling Resistance":
            //                            worksheet_1.Cells["H3"].Value = "ASTM D3512/D3512M-22";
            //                            worksheet_1.Cells["D4"].Value = "30";
            //                            break;
            //                    }
            //                    var MapPS = ExcelCrazyLineMapper.MapPS(Item.ItemName);
            //                    string[] samples = Item.Sample!.Split(',');
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_1.Cells[MapPS[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                    }

            //                }
            //                else if (Item.ItemName == "Seam Slippage")
            //                {
            //                    var worksheet_2 = PackagePhy.Workbook.Worksheets[2];
            //                    worksheet_2.Cells["M1"].Value = ordernumber;
            //                    worksheet_2.Cells["A3"].Value = "ASTM D1683/D1683M-22";
            //                    var MapSeamSlippage = ExcelCrazyLineMapper.MapSeamSlippage();
            //                    string[] samples = Item.Sample!.Split(',');
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_2.Cells[MapSeamSlippage[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                    }
            //                }
            //                else if (Item.ItemName == "Zipper Strength")
            //                {
            //                    var worksheet_Zipper = PackagePhy.Workbook.Worksheets[3];
            //                    if (sampleDescription!.Contains("EN"))
            //                    {
            //                        worksheet_Zipper = PackagePhy.Workbook.Worksheets[4];
            //                        worksheet_Zipper.Cells["A3"].Value = "EN 16732:2025";
            //                    }else if(sampleDescription!.Contains("ASTM"))
            //                    {
            //                        worksheet_Zipper = PackagePhy.Workbook.Worksheets[3];
            //                        worksheet_Zipper.Cells["A3"].Value = "ASTM D2061-07(2021)";
            //                    }
            //                    worksheet_Zipper.Cells["M1"].Value = ordernumber;
            //                    var Map = ExcelCrazyLineMapper.MapRegular();
            //                    string[] samples = Item.Sample!.Split(',');
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_Zipper.Cells[Map[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                    }
            //                }
            //                else if (Item.ItemName == "Small Parts")
            //                {
            //                    var worksheet_5 = PackagePhy.Workbook.Worksheets[5];
            //                    worksheet_5.Cells["M1"].Value = ordernumber;
            //                    var Map = ExcelCrazyLineMapper.MapRegular();
            //                    string[] samples = Item.Sample!.Split(',');
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_5.Cells[Map[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
            //                    }
            //                }
            //                else if (Item.ItemName == "Resistance to Unsnapping of Snap Fasteners")
            //                {
            //                    var worksheet_6 = PackagePhy.Workbook.Worksheets[6];
            //                    worksheet_6.Cells["A3"].Value = "ASTM D4846-96(2021)";
            //                    var Map = ExcelCrazyLineMapper.MapRegular();
            //                    string[] samples = Item.Sample!.Split(',');
            //                    for (int i = 0; i < samples.Length; i++)
            //                    {
            //                        worksheet_6.Cells[Map[i]].Value = samples[i].Trim(); // 假设从 G1 开始存放样本值
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

            //    PackageWet.Save();
            //    PackagePhy.Save();
            #endregion

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
            //<-------------------------------------------------------------------------------------->
            string? tplName = null;
            bool foundInSub = false;
            // 1) 模板 sheet
            if (TemplateSheetNames.TryGetValue(itemName, out var subDictionary))
            {
                foreach (var kvp in subDictionary)
                {

                    if (kvp.Key == null ||
                          kvp.Key != null && dto.sampleDescription!.Contains(kvp.Key))
                    {
                        tplName = kvp.Value;
                        foundInSub = true;
                        break;
                    }
                }
            }
            //如果在 TemplateSheetNames 中未找到，尝试从 TemplateSheetNamesNormal 中查找
            if (!foundInSub)
            {
                TemplateSheetNamesNormal.TryGetValue(itemName, out tplName);
            }

            // 如果仍未找到匹配的模板名
            if (tplName == null)
            {
                Console.WriteLine("未找到对应的模板名");
                tplName = "DefaultSheetName"; // 假设有一个默认模板名
            }
            var template = pkg.Workbook.Worksheets[tplName];
            //<-------------------------------------------------------------------------------------->

            // 2) 计算需要几张 sheet
            var cellAddrs = CellMapper[itemName](itemName, dto.sampleDescription!);
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            int offset = 0;
            if (dto.sampleDescription!.Contains("Fabric"))
            {
                offset = OffsetRule.GetValueOrDefault(itemName, 0);
            }// 获取偏移量，默认为0
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            int sheetCnt = (int)Math.Ceiling(samples.Length / (double)capacity);


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
                    ws = pkg.Workbook.Worksheets.Copy(tplName, newSheetName);
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

                /* 把这段样本写进去 */
                WriteSamples(ws, slice, cellAddrs, itemName, dto.sampleDescription!);

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
            ["Spriality/Skewing"] = "Spirality",
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
            ["Spriality/Skewing"] = new Dictionary<string, string>
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
            ["Spriality/Skewing"] = (_, m) => ExcelCrazyLineMapper.MapSpirality(m),
            ["Weight"] = (_, _) => ExcelCrazyLineMapper.MapWeight(),
            ["Pilling Resistance"] = (n, _) => ExcelCrazyLineMapper.MapPS(n),
            ["Seam Slippage"] = (_, _) => ExcelCrazyLineMapper.MapSeamSlippage(),
            ["Snagging Resistance"] = (n, _) => ExcelCrazyLineMapper.MapPS(n),
            ["Zipper Strength"] = (_, _) => ExcelCrazyLineMapper.MapRegular(),
            ["Resistance to Snapping of Snap Fasteners"] = (_, _) => ExcelCrazyLineMapper.MapRegular(),
            ["Resistance to Unsnapping of Snap Fasteners"] = (_, _) => ExcelCrazyLineMapper.MapRegular(),
            ["Small Parts"] = (_, _) => ExcelCrazyLineMapper.MapRegular(),
        };

        private static readonly Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>>> WetExtraMap = new()
        {
            ["DS to Washing"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>();
                if (w.WashingProcedure!.Contains("Machine"))
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["A3"] = (w, dto, reportNo) => "AATCC TM 135-2018t";
                    map["A5"] = (w, dto, reportNo) => w.Cycle + " Cycle";
                    map["V4"] = (w, dto, reportNo) => w.Temperature!;
                    map["E4"] = (w, dto, reportNo) => w.Program!;
                    map["M5"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["K4"] = (w, dto, reportNo) => w.DryCondition!;

                    map["A8"] = (w, dto, reportNo) => w.SpecialCareInstruction ?? null;

                }
                else if (w.WashingProcedure.Contains("Hand"))
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["A3"] = (w, dto, reportNo) =>
                    (dto.sampleDescription!.Contains("Socks") || dto.sampleDescription!.Contains("Gloves") || dto.sampleDescription!.Contains("Cap"))
                    == true ? "AATCC TM 150-2018t/AATCC TS006" : "AATCC TM 135-2018t/AATCC TS006";
                    map["H7"] = (w, dto, reportNo) => w.Temperature!;
                    map["M7"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["A8"] = (w, dto, reportNo) => w.SpecialCareInstruction ?? null;
                }
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["L14"] = (w, dto, reportNo) => w.AfterWash.ToString()!;
                    map["AF14"] = (w, dto, reportNo) => w.AfterWash.ToString()!;
                    map["L25"] = (w, dto, reportNo) => w.AfterWash.ToString()!;
                    map["AF25"] = (w, dto, reportNo) => w.AfterWash.ToString()!;
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["W9"] = (w, dto, reportNo) => w.AfterWash.ToString()!;
                    map["AB9"] = (w, dto, reportNo) => w.AfterWash.ToString()!;
                    map["AG11"] = (w, dto, reportNo) => w.AfterWash.ToString()!;
                    map["AL11"] = (w, dto, reportNo) => w.AfterWash.ToString()!;
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
                ["J5"] = (w, dto, reportNo) => w!.WashingProcedure!.Contains("Hand Wash Cold") ? "Rubbow" : "Steel"
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
            ["Spriality/Skewing"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>();
                map["P1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.sampleDescription!.Contains("Garment") == true ? "AATCC TM 179-2023, Method 2, Option 3" : "AATCC TM 179-2023, Method 1, Option 1";
                map["C5"] = (w, dto, reportNo) => "3";
                if (w.WashingProcedure!.Contains("Machine"))
                {
                    map["O31"] = (w, dto, reportNo) => "AATCC TM 179-2023";
                    map["D32"] = (w, dto, reportNo) => w.Program!;
                    map["I32"] = (w, dto, reportNo) => w.DryCondition!;
                    map["U32"] = (w, dto, reportNo) => w.Temperature!;
                    map["A33"] = (w, dto, reportNo) => w.Cycle!;
                    map["M33"] = (w, dto, reportNo) => w.DryProcedure!;
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
            string[] cellAddrs,
            string itemName,
            string sampleDescription)
        {
            int offset = 0;
            if (sampleDescription.Contains("Fabric"))
            {
                offset = OffsetRule.GetValueOrDefault(itemName, 0);
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
