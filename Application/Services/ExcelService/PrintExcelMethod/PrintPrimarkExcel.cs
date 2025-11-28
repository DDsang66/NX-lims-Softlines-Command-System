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



namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.PrintExcelMethod
{
    public sealed class PrintPrimarkExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintPrimarkExcel(LabDbContextSec db)
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
                    Extra = row.extra,
                    MenuName = menu,
                    sampleDescription = sampleDescription,
                });
            }
            foreach (var row in checkLists)
            {
                if (new[] { "Seam Slippage", "Seam Strength", "Tear Strength", "Tensile Strength", "Martindale Abrasion" , "Back Pocket Application Strength",
                "Belt Loop Application Strength"}
                     .Contains(row.ItemName))
                    checkLists.Add(new CheckListDto
                    {
                        ItemName = "Mass per Unit Area",
                        Standard = "BS EN 12127:1998",
                        Parameter = "Single unit weight",
                        Type = "Physics",
                        Sample = row.Sample,
                        Extra = null,
                        MenuName = menu,
                        sampleDescription = sampleDescription,
                    });
                break;
            }

            foreach (var dto in checkLists)
            {
                Console.WriteLine($"{dto.ItemName} -> {dto.Type}");
                var pkg = dto.Type == "Wet" ? PackageWet : PackagePhy;
                if (TemplateSheetNames.ContainsKey(dto.ItemName!)|| TemplateSheetNamesNormal.ContainsKey(dto.ItemName!))
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
            if (TemplateSheetNames.TryGetValue(itemName!, out var subDictionary))
            {
                /* ---------- 其余测试保持原单关键字逻辑 ---------- */
                foreach (var kvp in subDictionary)
                {
                    if (string.IsNullOrEmpty(kvp.Key) ||
                        dto.sampleDescription!.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
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
            if (itemName == "Physical & Mechanical" || itemName == "Torque & Tension") 
            {
                switch (itemName) 
                {
                    case "Physical & Mechanical":
                        if (dto.Standard!.Contains("EN 71-1:2014+A1:2018 8.4")) tplName = "Attachment Strength";
                        else if( dto.Standard!.Contains("ASTM F963-23")) tplName = "ASTM F963-23";
                        break;
                    case "Torque & Tension":
                        if (dto.Standard!.Contains("16 CFR 1500.51-53")) tplName = "Torque&Tension";
                        else if (dto.Standard!.Contains("EN 71-1:2024+A1:2018")) tplName = "Attachment Strength";
                        break;  
                }
            }

            var template = pkg.Workbook.Worksheets[tplName];
            //<-------------------------------------------------------------------------------------->

            // 2) 计算需要几张 sheet
            var cellAddrs = CellMapper[itemName](itemName, dto.Standard!,dto.sampleDescription!);
            string[]? AfterWashCellAddrs = null;
            if (itemName == "Dimensional Stability" ||
                itemName == "Stability to Dry Cleaning" ||
                itemName == "Stability to Washing" ||
                itemName == "Appearance-Common" ||
                itemName == "Security of Attachment(Wash)"||
                itemName == "Easycare/Non-Iron"||
                (itemName == "Appearance" && dto.Standard != "PM01") ||
                (itemName == "Spirality" && dto.Standard != "PM01"))
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName,dto.Standard!,dto.sampleDescription!);
            }


            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            if (itemName == "Spirality" && dto.Standard == "PM01")
            {
                samples = dto.Sample!
                    .Split(',')
                    .Select(s => s.Trim())
                    .SelectMany(s => new[] { $"{s} - 5 Wash", $"{s} - 23 Wash", $"{s} - 32 Wash", $"{s} - 45 Wash" })
                    .ToArray();
            }

            int[]? afterWashMap = null;
            if (itemName == "Dimensional Stability" ||
                itemName == "Stability to Dry Cleaning" ||
                itemName == "Stability to Washing" ||
                itemName == "Appearance-Common"||
                itemName == "Security of Attachment(Wash)" ||
                itemName == "Easycare/Non-Iron" ||
                (itemName == "Appearance"&&dto.Standard!="PM01") ||
                (itemName == "Spirality" && dto.Standard != "PM01"))
            {
                var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                if (wp == null) wp = new WetParameterIso();
                string? afterWash = wp!.AfterWash;

                if (itemName == "Dimensional Stability")
                {
                    afterWash = string.Join(", ", dto.Sample!
                        .Split(',')
                        .Select(s => s.Trim())
                        .SelectMany(s => new[] { $"{s}-5 Wash-23 Wash-32 Wash-45 Wash" }));
                }
                string? iron = wp!.Iron;
                samples = SampleNumCounter.GetSample(dto.Sample!, afterWash, iron);
                afterWashMap = SampleNumCounter.ExpandWashNumbers(samples!, afterWash!, iron);
            }
            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            int offset = 0; // 假设没有偏移
            offset = OffsetRule.GetValueOrDefault(itemName, 0);
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            if (itemName == "Colour Fastness to Hot Pressing") { capacity = 3; }// 特例处理，实际容量为3
            if (itemName == "Appearance"||itemName== "Appearance-Common") { capacity = 1; }
            if ((itemName == "Dimensional Stability"||itemName=="Stability to Washing")&& !dto.sampleDescription!.Contains("Fabric")) { capacity = 1; }
            if (itemName == "Easycare/Non-Iron") { capacity = 1; }
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
                WriteSamples(ws, slice, afmap, cellAddrs, AfterWashCellAddrs, itemName, dto.sampleDescription!,dto.Standard!);
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
                    var extraMap = PhyExtraMap.GetValueOrDefault(itemName, (wp, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>())(wp, dto, reportNo);
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
            ["Abrasion of Knitted Footwear Garments - Modified Martindale"]= "Abrasion",
            ["Absorbency of Textiles"]= "Absorbency",
            ["Accelerotor"] = "Accelerotor",
            ["Back Pocket Application Strength"]= "PM07PM08",
            ["Belt Loop Application Strength"] = "PM07PM08",
            ["Chenille Pile Loss"]= "PM06",
            ["Elastic Extension and Modulus Test"]= "PM23&AR(TABER)",
            ["EU Security of Attachment on Children's Clothing"] = "Attachment Strength",
            ["Fibre Proof Properties"]= "Fibre Proof Properties",
            ["Fibre Shedding"]= "PM03PM05",
            ["Martindale Abrasion"]= "Abrasion",
            ["Martindale Pilling"] = "Pilling Resistance",
            ["Mass per Unit Area"]="Weight",
            ["Nap Stability"]= "PM03PM05",
            ["Peel Bond"]="Peel Bond",
            ["Pile Retention"]= "PM03PM05",
            ["Quick Dry"]= "DryingRate",
            ["Residual Elongation"]= "Stretch&Recovery of Elastic",
            ["Residual Elongation SHAPEWEAR"]= "Stretch&Recovery of Elastic",
            ["Security of Attachment"]= "Attachment Strength",
            ["Security of Attachment Buttons"]= "Attachment Strength",
            ["Security of Attachment Mechanically Applied Fasteners"]="Attachment Strength",
            ["Sharp Edges Restrctions"]= "Torque&Tension",
            ["Sharp Point Restrctions"]= "Torque&Tension",
            ["Small Parts Restrictions"]= "Torque&Tension",
            ["Shower Resistant Claims Spray Rating"] = "WaterRepellency",
            ["Tear Strength"]= "Tearing Strength",
            ["Tensile Strength"] = "Tensile Strength",
            ["Unrecovered Elongation"] = "Stretch&Recovery of Elastic",
            ["Waterproof Claims Hydrostatic Head"] = "Hydrostatichead",
            ["Wind Resistant Claims Air Permeability"] = "Air Permeability",
            ["Zip Fasteners"]= "ZipperStrength",
            ["Vertical Wicking of Textiles"] = "Wicking",

            ["Colour Fastness to Chlorinated Water"] = "CFtoSublimation&HotPressing&Cl",
            ["Colour Fastness to Chlorine Bleach"] = "CFtoPerspiration&Bleach",
            ["Colour Fastness to Dry Cleaning"] = "Yellowing&DryClean",
            ["Colour Fastness to Hot Pressing"] = "CFtoSublimation&HotPressing&Cl",
            ["Colour Fastness to Light"] = "CFtoWash&Rub&Lig&Wat",
            ["Colour Fastness to Non Chlorine Bleach"] = "CFtoPerspiration&Bleach",
            ["Colour Fastness to Perspiration"] = "CFtoPerspiration&Bleach",
            ["Colour Fastness to PVC Migration"] = "CFtoSeaWater&PVC",
            ["Colour Fastness to Rubbing"] = "CFtoWash&Rub&Lig&Wat",
            ["Colour Fastness to Saliva"] = "CFtoSaliva&Sweat",
            ["Colour Fastness to Saliva and Perspiration"] = "CFtoSaliva&Sweat",
            ["Colour Fastness to Sea Water"] = "CFtoSeaWater&PVC",
            ["Colour Fastness to Washing"] = "CFtoWash&Rub&Lig&Wat",
            ["Colour Fastness to Water"] = "CFtoWash&Rub&Lig&Wat",
            ["Dimensional and Bra Wire Casing Stability"] = "BraWireCasing",
            ["Dye Transfer in Storage"] = "TSBoardFit&DyeTransfer",
            ["Easycare/Non-Iron"] = "Easycare&Non-Iron",
            ["Phenolic Yellowing"] = "Yellowing&DryClean",
            ["Print / Motif / Flock Durability"] = "Print&Motif&Flock",
            ["Print Durability"] = "Print&Motif&Flock",
            ["Security of Attachment(Wash)"] = "Determination of FC",
            ["Stability to Dry Cleaning"] = "StabilitytoDryClean",
            ["TS Board Fit"] = "TSBoardFit&DyeTransfer",
            ["Appearance"] = "Appearance-PM01",
            ["Appearance-Common"] = "Appearance-Common",
            ["Colour Change and Staining"] = "Appearance-PM01",
            };

        private static readonly Dictionary<string, Dictionary<string, string>> TemplateSheetNames = new()
        {
            [("Seam Slippage")] = new Dictionary<string, string>
            {
                {"Fabric", "Seam Slippage&Strength" },
                {"Garment","Seam Slippage&Strength-G"},
            },
            [("Seam Strength")] = new Dictionary<string, string>
            {
                {"Fabric", "Seam Slippage&Strength" },
                {"Garment","Seam Slippage&Strength-G"},
            },
            [("Bursting Strength")] = new Dictionary<string, string>
            {
                {"Fabric", "Bursting Strength" },
                {"Garment","Bursting Strength-G"},
            },
            [("Physical & Mechanical")] = new Dictionary<string, string>
            {
                {"EN 71-1:2014+A1:2018 8.4", "Attachment Strength" },
            },
            [("Physical & Mechanical")] = new Dictionary<string, string>
            {
                {"ASTM F963-23", "ASTM F963-23" },
            },
            [("Torque & Tension")] = new Dictionary<string, string>
            {
                {"16 CFR 1500.51-53", "Torque&Tension" },
            },
            [("Torque & Tension")] = new Dictionary<string, string>
            {
                {"EN 71-1:2024+A1:2018", "Attachment Strength" },
            },
            [("Spirality")] = new Dictionary<string, string>
            {
                {"Fabric", "Spirality-F" },
                {"Garment", "Spirality-G" },
            },
            [("Dimensional Stability")] = new Dictionary<string, string>
            {
                {"Fabric", "DStoWashing-F" },
                {"Garment", "DStoWashing-G" },
                {"Socks", "DStoWashing-Acc" },
                {"Gloves", "DStoWashing-Acc" },
                {"Cap", "DStoWashing-Acc" },
            },
            [("Stability to Washing")] = new Dictionary<string, string>
            {
                {"Fabric", "DStoWashing-F" },
                {"Garment", "DStoWashing-G" },
                {"Socks", "DStoWashing-Acc" },
                {"Gloves", "DStoWashing-Acc" },
                {"Cap", "DStoWashing-Acc" },
            },
        };
        private static readonly Dictionary<string, Func<string, string, string, string[]>> CellMapper = new()
        {
            ["Abrasion of Knitted Footwear Garments - Modified Martindale"] = (n, m, l) => ExcelPrimarkMapper.MapAbrasion(m),
            ["Absorbency of Textiles"] = (n, m, l) => ExcelPrimarkMapper.MapAbsorbency(),
            ["Accelerotor"] = (n, m, l) => ExcelPrimarkMapper.MapAccelerotor(),
            ["Back Pocket Application Strength"] = (n, m, l) => ExcelPrimarkMapper.MapPM07PM08(m),
            ["Belt Loop Application Strength"] = (n, m, l) => ExcelPrimarkMapper.MapPM07PM08(m),
            ["Chenille Pile Loss"] = (n, m, l) => ExcelPrimarkMapper.MapPM06(),
            ["Elastic Extension and Modulus Test"] = (n, m, l) => ExcelPrimarkMapper.MapPM23TABER(m),
            ["EU Security of Attachment on Children's Clothing"] = (n, m, l) => ExcelPrimarkMapper.MapAttachmentStrength(),
            ["Fibre Proof Properties"] = (n, m, l) => ExcelPrimarkMapper.MapFibreProof(),
            ["Fibre Shedding"] = (n, m, l) => ExcelPrimarkMapper.MapPM03PM05(m),
            ["Martindale Abrasion"] = (n, m, l) => ExcelPrimarkMapper.MapAbrasion(m),
            ["Martindale Pilling"] = (n, m, l) => ExcelPrimarkMapper.MapPilling(),
            ["Mass per Unit Area"] = (n, m, l) => ExcelPrimarkMapper.MapWeight(),
            ["Nap Stability"] = (n, m, l) => ExcelPrimarkMapper.MapPM03PM05(m),
            ["Peel Bond"] = (n, m, l) => ExcelPrimarkMapper.MapPeelBond(),
            ["Pile Retention"] = (n, m, l) => ExcelPrimarkMapper.MapPM03PM05(m),
            ["Quick Dry"] = (n, m, l) => ExcelPrimarkMapper.MapDryRate(),
            ["Residual Elongation"] = (n, m, l) => ExcelPrimarkMapper.MapElastic(),
            ["Residual Elongation SHAPEWEAR"] = (n, m, l) => ExcelPrimarkMapper.MapElastic(),
            ["Security of Attachment"] = (n, m, l) => ExcelPrimarkMapper.MapAttachmentStrength(),
            ["Security of Attachment Buttons"] = (n, m, l) => ExcelPrimarkMapper.MapAttachmentStrength(),
            ["Security of Attachment Mechanically Applied Fasteners"] = (n, m, l) => ExcelPrimarkMapper.MapAttachmentStrength(),
            ["Sharp Edges Restrctions"] = (n, m, l) => ExcelPrimarkMapper.MapTorqueTension(m),
            ["Sharp Point Restrctions"] = (n, m, l) => ExcelPrimarkMapper.MapTorqueTension(m),
            ["Small Parts Restrictions"] = (n, m, l) => ExcelPrimarkMapper.MapTorqueTension(m),
            ["Shower Resistant Claims Spray Rating"] = (n, m, l) => ExcelPrimarkMapper.MapRepellency(l),
            ["Tear Strength"] = (n, m, l) => ExcelPrimarkMapper.MapTear(),
            ["Tensile Strength"] = (n, m, l) => ExcelPrimarkMapper.MapTensile(),
            ["Unrecovered Elongation"] = (n, m, l) => ExcelPrimarkMapper.MapElastic(),
            ["Waterproof Claims Hydrostatic Head"] = (n, m, l) => ExcelPrimarkMapper.MapHydroatatic(),
            ["Wind Resistant Claims Air Permeability"] = (n, m, l) => ExcelPrimarkMapper.MapAir(),
            ["Zip Fasteners"] = (n, m, l) => ExcelPrimarkMapper.MapZipper(),
            ["Vertical Wicking of Textiles"] = (n, m, l) => ExcelPrimarkMapper.MapWicking(),
            ["Bursting Strength"] = (n, m, l) => ExcelPrimarkMapper.MapBursting(l),
            ["Seam Slippage"] = (n, m, l) => ExcelPrimarkMapper.MapSlippageStrength(n,l),
            ["Seam Strength"] = (n, m, l) => ExcelPrimarkMapper.MapSlippageStrength(n,l),
            ["Physical & Mechanical"] = (n, m, l) => ExcelPrimarkMapper.MapPhysicalMechanical(m),
            ["Torque & Tension"] = (n, m, l) => ExcelPrimarkMapper.MapTorqueTension(m),


            ["Colour Fastness to Chlorinated Water"] = (n,m , l) => ExcelPrimarkMapper.MapSPC(n),
            ["Colour Fastness to Chlorine Bleach"] = (n, m, l) => ExcelPrimarkMapper.MapPB(n),
            ["Colour Fastness to Dry Cleaning"] = (n, m, l) => ExcelPrimarkMapper.MapYD(n),
            ["Colour Fastness to Hot Pressing"] = (n, m, l) => ExcelPrimarkMapper.MapSPC(n),
            ["Colour Fastness to Light"] = (n, m, l) => ExcelPrimarkMapper.MapWRLW(n),
            ["Colour Fastness to Non Chlorine Bleach"] = (n, m, l) => ExcelPrimarkMapper.MapPB(n),
            ["Colour Fastness to Perspiration"] = (n, m, l) => ExcelPrimarkMapper.MapPB(n),
            ["Colour Fastness to PVC Migration"] = (n, m, l) => ExcelPrimarkMapper.MapSeaWaterPVC(n),
            ["Colour Fastness to Rubbing"] = (n, m, l) => ExcelPrimarkMapper.MapWRLW(n),
            ["Colour Fastness to Saliva"] = (n, m, l) => ExcelPrimarkMapper.MapCFtoSalivaSweat(),
            ["Colour Fastness to Saliva and Perspiration"] = (n, m, l) => ExcelPrimarkMapper.MapCFtoSalivaSweat(),
            ["Colour Fastness to Sea Water"] = (n, m, l) => ExcelPrimarkMapper.MapSeaWaterPVC(n),
            ["Colour Fastness to Washing"] = (n, m, l) => ExcelPrimarkMapper.MapWRLW(n),
            ["Colour Fastness to Water"] = (n, m, l) => ExcelPrimarkMapper.MapWRLW(n),
            ["Dimensional and Bra Wire Casing Stability"] = (n, m, l) => ExcelPrimarkMapper.MapBra(),
            ["Dye Transfer in Storage"] = (n, m, l) => ExcelPrimarkMapper.MapCFtoTD(n),
            ["Easycare/Non-Iron"] = (n, m, l) => ExcelPrimarkMapper.MapCFtoEI(m),
            ["Phenolic Yellowing"] = (n, m, l) => ExcelPrimarkMapper.MapYD(n),
            ["Print / Motif / Flock Durability"] = (n, m, l) => ExcelPrimarkMapper.MapDurability(),
            ["Print Durability"] = (n, m, l) => ExcelPrimarkMapper.MapDurability(),
            ["Security of Attachment(Wash)"] = (n, m, l) => ExcelPrimarkMapper.MapAttachment(),
            ["Stability to Dry Cleaning"] = (n, m, l) => ExcelPrimarkMapper.MapStabilityToDryClean(),
            ["TS Board Fit"] = (n, m, l) => ExcelPrimarkMapper.MapCFtoTD(n),
            ["Appearance"] = (n, m, l) => ExcelPrimarkMapper.MapAppearance(m),
            ["Appearance-Common"] = (n, m, l) => ExcelPrimarkMapper.MapAppearance(m),
            ["Colour Change and Staining"] = (n, m, l) => ExcelPrimarkMapper.MapAppearance(m),
            ["Spirality"] = (n, m, l) => ExcelPrimarkMapper.MapSpirality(m),
            ["Dimensional Stability"] = (n, m, l) => ExcelPrimarkMapper.MapStability(l),
            ["Stability to Washing"] = (n, m, l) => ExcelPrimarkMapper.MapStability(l),
        };
        //取洗涤遍数映射地址的函数
        private static readonly Dictionary<string, Func<string,string, string, string[]>> AfterWashCellMapper = new()
        {
            ["Dimensional Stability"] = (n, m, l) => ExcelPrimarkMapper.StabilityAf(l),
            ["Stability to Washing"] = (n, m, l) => ExcelPrimarkMapper.StabilityAf(l),
            ["Stability to Dry Cleaning"] = (n, m, l) => ExcelPrimarkMapper.DStoDCAf(),
            ["Print / Motif / Flock Durability"] = (n, m, l) => ExcelPrimarkMapper.DurabilityAf(),
            ["Print Durability"] = (n, m, l) => ExcelPrimarkMapper.DurabilityAf(),
            ["Security of Attachment(Wash)"] = (n, m, l) => ExcelPrimarkMapper.AttachmentAf(),
            ["Easycare/Non-Iron"] = (n, m, l) => ExcelPrimarkMapper.EasyCareAf(m),
            ["Appearance-Common"] = (n, m, l) => ExcelPrimarkMapper.AppearanceAf(),
        };
        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, string, Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>>> WetExtraMap = new()
        {
            ["Colour Fastness to Chlorinated Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["H1"] = (w, dto, reportNo) => reportNo,
                ["A27"] = (w, dto, reportNo) => dto.Standard!,
                ["E28"] = (w, dto, reportNo) => dto.Parameter!,
            },
            ["Colour Fastness to Chlorine Bleach"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A29"] = (w, dto, reportNo) => dto.Standard!,
                ["L30"] = (w, dto, reportNo) => dto.Parameter == "N/A" ? "N/A" : "-",
            },
            ["Colour Fastness to Dry Cleaning"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR12"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Colour Fastness to Hot Pressing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["H1"] = (w, dto, reportNo) => reportNo,
                ["A12"] = (w, dto, reportNo) => dto.Standard!,
                ["G13"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod) ? "/" : w.Temperature!,
                ["R13"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod) ? "N/A" : "-",
            },
            ["Colour Fastness to Light"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A28"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Colour Fastness to Non Chlorine Bleach"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A29"] = (w, dto, reportNo) => dto.Standard!,
                ["L30"] = (w, dto, reportNo) =>dto.Parameter == "N/A"? "N/A" : "-",
            },
            ["Colour Fastness to Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Colour Fastness to PVC Migration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Colour Fastness to Rubbing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A20"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Colour Fastness to Saliva"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["G3"] = (w, dto, reportNo) => "√"
            },
            ["Colour Fastness to Saliva and Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["G3"] = (w, dto, reportNo) => "√"
            },
            ["Colour Fastness to Sea Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A10"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Colour Fastness to Washing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["B4"] = (w, dto, reportNo) => w.Program!,
                ["E4"] = (w, dto, reportNo) => w.Temperature!,
                ["L5"] = (w, dto, reportNo) => w.SteelBallNum.ToString()!,
            },
            ["Colour Fastness to Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A35"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Dimensional and Bra Wire Casing Stability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Dye Transfer in Storage"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
                ["AY4"] = (w, dto, reportNo) => "30",
                ["BE4"] = (w, dto, reportNo) => "48"
            },
            ["Easycare/Non-Iron"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["BC1"] = (wp, dto, reportNo) => reportNo;
                switch (dto.Standard) 
                {
                    case "AATCC TM124-2018te":
                        map["AR4"] = (wp, dto, reportNo) => dto.Standard!;
                        break;
                    case "ISO7769:2009":
                        map["AR23"] = (wp, dto, reportNo) => dto.Standard!;
                        break;
                }
                return map;
            },
            ["Phenolic Yellowing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Print / Motif / Flock Durability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
                ["AU48"] = (w, dto, reportNo) => w.DryProcedure!,
            },
            ["Print Durability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
                ["AU48"] = (w, dto, reportNo) => w.DryProcedure!,
            },
            ["Security of Attachment(Wash)"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR4"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Stability to Dry Cleaning"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AW4"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal"
            },
            ["TS Board Fit"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR19"] = (w, dto, reportNo) => dto.Standard!
            },
            ["Appearance"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,  ["CM1"] = (w, dto, reportNo) => reportNo,
                ["BC57"] = (w, dto, reportNo) => reportNo,["CM57"] = (w, dto, reportNo) => reportNo,
                ["BC114"] = (w, dto, reportNo) => reportNo, ["CM114"] = (w, dto, reportNo) => reportNo,
                ["BC171"] = (w, dto, reportNo) => reportNo, ["CM171"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01"!,  ["CB3"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",
                ["AR59"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",  ["CB59"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",
                ["AR116"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",  ["CB116"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",
                ["AR173"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",  ["CB173"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",
                ["C1"] = (w, dto, reportNo) => dto.Parameter!,
            },
            ["Colour Change and Staining"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
            },
            ["Dimensional Stability"] = (w, dto, reportNo) => 
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>(); 
                if (dto.sampleDescription!.Contains("Fabric")) 
                {
                    map["BC1"] = (wp, dto, reportNo) => reportNo;
                    map["AR3"] = (wp, dto, reportNo) => "BS EN ISO 5077:2008/BS EN ISO 3759:2011/BS EN ISO 6330:2021";
                    map["AX4"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["BX4"] = (wp, dto, reportNo) => w.Temperature!;
                    map["BF5"] = (wp, dto, reportNo) => w.Ballast!;
                    map["BI6"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["BR6"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                    map["AR7"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["P1"] = (wp, dto, reportNo) => reportNo;
                    map["A3"] = (wp, dto, reportNo) => "BS EN ISO 5077:2008/BS EN ISO 3759:2011/BS EN ISO 6330:2021";
                    map["I4"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["AJ4"] = (wp, dto, reportNo) => w.Temperature!;
                    map["S5"] = (wp, dto, reportNo) => w.Ballast!;
                    map["V6"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["AE6"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                    map["A7"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                if (dto.sampleDescription!.Contains("Cap")|| dto.sampleDescription!.Contains("Socks")|| dto.sampleDescription!.Contains("Gloves"))
                {
                    map["N1"] = (wp, dto, reportNo) => reportNo;
                    map["A3"] = (wp, dto, reportNo) => "BS EN ISO 5077:2008/BS EN ISO 3759:2011/BS EN ISO 6330:2021";
                    map["G4"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["AL4"] = (wp, dto, reportNo) => w.Temperature!;
                    map["R5"] = (wp, dto, reportNo) => w.Ballast!;
                    map["T6"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["AD6"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                    map["A7"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Stability to Washing"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["BC1"] = (wp, dto, reportNo) => reportNo;
                    map["AR3"] = (wp, dto, reportNo) => dto.Standard!;
                    map["AX4"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["BX4"] = (wp, dto, reportNo) => w.Temperature!;
                    map["BF5"] = (wp, dto, reportNo) => w.Ballast!;
                    map["BI6"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["BR6"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                    map["AR7"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["P1"] = (wp, dto, reportNo) => reportNo;
                    map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                    map["I4"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["AJ4"] = (wp, dto, reportNo) => w.Temperature!;
                    map["S5"] = (wp, dto, reportNo) => w.Ballast!;
                    map["V6"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["AE6"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                    map["A7"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                if (dto.sampleDescription!.Contains("Cap") || dto.sampleDescription!.Contains("Socks") || dto.sampleDescription!.Contains("Gloves"))
                {
                    map["N1"] = (wp, dto, reportNo) => reportNo;
                    map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                    map["G4"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["AL4"] = (wp, dto, reportNo) => w.Temperature!;
                    map["R5"] = (wp, dto, reportNo) => w.Ballast!;
                    map["T6"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["AD6"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                    map["A7"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Spirality"] = (w, dto, reportNo) => 
            { 
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["P1"] = (w, dto, reportNo) => reportNo;
                if(dto.sampleDescription!.Contains("Fabric")) map["A3"] = (w, dto, reportNo) => "BS EN ISO 16322-2:2021,Method A"!;
                else if(dto.sampleDescription!.Contains("Garment")) map["A3"] = (w, dto, reportNo) => "BS EN ISO 16322-3:2021,Procedure B"!;
                return map;
            },
        };
        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, string, Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>>> PhyExtraMap = new()
        {
            ["Abrasion of Knitted Footwear Garments - Modified Martindale"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A21"] = (w, dto, reportNo) => dto.Standard!,
                ["C25"] = (w, dto, reportNo) => "12KPa",
                ["I25"] = (w, dto, reportNo) => "8000 revs",
            },
            ["Absorbency of Textiles"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.Standard!;
                map["A31"] = (w, dto, reportNo) => w.Bleach + " Cycle";
                map["S30"] = (w, dto, reportNo) => w.Temperature!;
                map["E30"] = (w, dto, reportNo) => w.Program!;
                map["R31"] = (w, dto, reportNo) => w.DryProcedure!;
                map["H30"] = (w, dto, reportNo) => w.DryCleanProcedure!;
                map["AF31"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                map["A32"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["A29"] = (w, dto, reportNo) => "AATCC TM 135-2018t";
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["A29"] = (w, dto, reportNo) => "AATCC TM 150-2018t/AATCC TS006";
                }
                return map;
            },
            ["Accelerotor"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["N5"] = (w, dto, reportNo) => "2000",
                ["AF5"] = (w, dto, reportNo) => dto.Parameter!.Contains("3") ? "3" : "5",
            },
            ["Back Pocket Application Strength"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
            },
            ["Belt Loop Application Strength"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
            },
            ["Chenille Pile Loss"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
            },
            ["Elastic Extension and Modulus Test"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
            },
            ["EU Security of Attachment on Children's Clothing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["A17"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Fibre Proof Properties"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Fibre Shedding"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
            },
            ["Martindale Abrasion"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["C5"] = (w, dto, reportNo) => "9KPa",
                ["A6"] = (w, dto, reportNo) => dto.Parameter!.Contains("@ 5000")
                ? "{<100g/m²：10000 rubs；101~199g/m²：15000 rubs；>2000g/m²：20000 rubs}"
                : "{<200g/m²：10000 rubs；201~270g/m²：15000 rubs；271~390g/m²：18000 rubs；>390g/m²：20000 rubs}",
                ["AA5"] = (w, dto, reportNo) => dto.Parameter!.Contains("@ 5000") ? "@ 5000 revs" : "-"
            },
            ["Martindale Pilling"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["F3"] = (w, dto, reportNo) => dto.Standard!,
                ["D4"] = (w, dto, reportNo) => dto.Parameter!.Contains("2000 revs")?"2000 revs":"500 revs",
                ["AC3"] = (w, dto, reportNo) => dto.Parameter!.Contains("N/A") ? "N/A" : "-",
                ["G40"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["AJ40"] = (w, dto, reportNo) => w.Temperature!,
                ["Q41"] = (w, dto, reportNo) => w.Ballast!,
                ["S42"] = (w, dto, reportNo) => w.DryProcedure!,
                ["AB42"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod) ? "/ Iron" : w.IronMethod!
            },
            ["Mass per Unit Area"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["S3"] = (w, dto, reportNo) => dto.Parameter!.Contains("unit")?"刻一个":"-",
            },
            ["Nap Stability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
            },
            ["Peel Bond"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Pile Retention"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
            },
            ["Quick Dry"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Residual Elongation"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.Standard!;
                map["AE9"] = (w, dto, reportNo) => dto.Parameter!.Contains("N/A") ? "N/A" : "-";
                if (dto.sampleDescription!.Contains("Woven"))
                {
                    map["A5"] = (w, dto, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Woven/Non-woven Fabric: method B---Loop trials Perimeter =200mm Speed =100mm/min"
                    : "Woven/Non-woven Fabric: method A---Stripe trials  Guage length=200mm  Speed =200mm/min.";
                }
                else if (dto.sampleDescription!.Contains("Knit"))
                {
                    map["A5"] = (w, dto, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Knitted Fabric: method B---Loop trials  Perimeter =200mm Speed =500mm/min" :
                    "Knitted Fabric: method A---Stripe trials Guage length=100mm Speed =500mm/min.";
                };
                map["L7"] = (w, dto, reportNo) => "5";
                map["F7"] = (w, dto, reportNo) => dto.Parameter!.Contains("15")?"15"
                : dto.Parameter.Contains("20")?"20"
                : dto.Parameter.Contains("25")?"25"
                : dto.Parameter.Contains("30")?"30":"40";
                return map;
            },
            ["Residual Elongation SHAPEWEAR"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.Standard!;
                map["AE9"] = (w, dto, reportNo) => dto.Parameter!.Contains("N/A") ? "N/A" : "-";
                if (dto.sampleDescription!.Contains("Woven"))
                {
                    map["A5"] = (w, dto, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Woven/Non-woven Fabric: method B---Loop trials Perimeter =200mm Speed =100mm/min"
                    : "Woven/Non-woven Fabric: method A---Stripe trials  Guage length=200mm  Speed =200mm/min.";
                }
                else if (dto.sampleDescription!.Contains("Knit"))
                {
                    map["A5"] = (w, dto, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Knitted Fabric: method B---Loop trials  Perimeter =200mm Speed =500mm/min" :
                    "Knitted Fabric: method A---Stripe trials Guage length=100mm Speed =500mm/min.";
                };
                map["L7"] = (w, dto, reportNo) => "5";
                map["F7"] = (w, dto, reportNo) => "36";
                return map;
            },
            ["Security of Attachment"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["A17"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Security of Attachment Buttons"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Security of Attachment Mechanically Applied Fasteners"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A17"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Sharp Edges Restrctions"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A4"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Sharp Point Restrctions"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A4"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Small Parts Restrictions"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A4"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Shower Resistant Claims Spray Rating"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["G19"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["AJ19"] = (w, dto, reportNo) => w.Temperature!,
                ["P20"] = (w, dto, reportNo) => w.Ballast!,
                ["S21"] = (w, dto, reportNo) => w.DryProcedure!,
                ["AB21"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod) ? "/ Iron" : w.IronMethod!
            },
            ["Tear Strength"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Tensile Strength"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Unrecovered Elongation"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.Standard!;
                map["AE9"] = (w, dto, reportNo) => dto.Parameter!.Contains("N/A") ? "N/A" : "-";
                if (dto.sampleDescription!.Contains("Woven"))
                {
                    map["A5"] = (w, dto, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Woven/Non-woven Fabric: method B---Loop trials Perimeter =200mm Speed =100mm/min"
                    : "Woven/Non-woven Fabric: method A---Stripe trials  Guage length=200mm  Speed =200mm/min.";
                }
                else if (dto.sampleDescription!.Contains("Knit"))
                {
                    map["A5"] = (w, dto, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Knitted Fabric: method B---Loop trials  Perimeter =200mm Speed =500mm/min" :
                    "Knitted Fabric: method A---Stripe trials Guage length=100mm Speed =500mm/min.";
                };
                map["L7"] = (w, dto, reportNo) => "5";
                map["F7"] = (w, dto, reportNo) => dto.Parameter!.Contains("30") ? "30" : "40";
                return map;
            },
            ["Waterproof Claims Hydrostatic Head"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["I8"] = (w, dto, reportNo) => dto.Parameter!.Contains("1600mm") ? "1600" 
                : dto.Parameter!.Contains("1000mm") ?"1000"
                : dto.Parameter!.Contains("10000mm") ? "10000"
                : dto.Parameter!.Contains("8000mm") ? "8000"
                :"/" ,
                ["I15"] = (w, dto, reportNo) => dto.Parameter!.Contains("1600mm") ? "1600"
                : dto.Parameter!.Contains("1000mm") ? "1000"
                : dto.Parameter!.Contains("10000mm") ? "10000"
                : dto.Parameter!.Contains("8000mm") ? "8000"
                : "/",
                ["G30"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["AJ30"] = (w, dto, reportNo) => w.Temperature!,
                ["P31"] = (w, dto, reportNo) => w.Ballast!,
                ["S32"] = (w, dto, reportNo) => w.DryProcedure!,
                ["AB32"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod) ? "/ Iron" : w.IronMethod!
                //洗前洗后都有
            },
            ["Zip Fasteners"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Vertical Wicking of Textiles"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Wind Resistant Claims Air Permeability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["A25"] = (w, dto, reportNo) => dto.Standard!,
                ["F5"] = (w, dto, reportNo) =>"100",
                ["E6"] = (w, dto, reportNo) => "20",
            },
            ["Physical & Mechanical"] = (w, dto, reportNo) => 
            {                
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                if (dto.Standard!.Contains("ASTM F963-23")) map["A3"] = (w, dto, reportNo) => dto.Standard!;
                else if (dto.Standard!.Contains( "EN 71-1:2014+A1:2018 8.4")) map["A17"] = (w, dto, reportNo) => dto.Standard!;
                return map;
            },
            ["Torque & Tension"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                if (dto.Standard == "EN 71-1:2014+A1:2018 8.4") map["A17"] = (w, dto, reportNo) => dto.Standard!;
                return map;
            },
            ["Bursting Strength"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("Fabric")) map["I3"] = (w, dto, reportNo) => dto.Standard!;
                else if (dto.sampleDescription!.Contains("Seam")) map["I18"] = (w, dto, reportNo) => dto.Standard!;
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    string? component = SeamExtraHelper.GetExtraField<string>(dto, "component", objIndex: 0);
                    string? layout = SeamExtraHelper.GetExtraField<string>(dto, "layout", objIndex: 0);

                    map["J3"] = (w, dto, reportNo) => dto.Standard!;
                    if (layout!.Contains("Shell") && !string.IsNullOrEmpty(layout)) map["Q4"] = (w, dto, reportNo) => "√";
                    if (layout.Contains("Lining") && !string.IsNullOrEmpty(layout)) map["AF4"] = (w, dto, reportNo) => "√";

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
                    // 2. 固定顺序的单元格列表
                    var cellOrder = new List<string>{
                        "A5", "A6", "A7", "A8", "A9", "A10","A11", "A12","A13","A14", "A15", "A16"
                    };
                    var selectedParts = (component ?? "")
                        .Split('-', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(k => descMap.ContainsKey(k))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // 4. 按顺序依次填，发完为止
                    for (int i = 0; i < selectedParts.Count && i < cellOrder.Count; i++)
                    {
                        string part = selectedParts[i];
                        string cell = cellOrder[i];
                        string desc = descMap[part];
                        map[cell] = (w, dto, reportNo) => desc;
                    }
                }
                    return map;
            },
            ["Seam Slippage"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["A3"] = (w, dto, reportNo) => dto.Standard!;
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    string? component = SeamExtraHelper.GetExtraField<string>(dto, "component", objIndex: 0);
                    string? layout = SeamExtraHelper.GetExtraField<string>(dto, "layout", objIndex: 0);

                    map["J3"] = (w, dto, reportNo) => dto.Standard!;
                    if (layout!.Contains("Shell") && !string.IsNullOrEmpty(layout)) map["Q4"] = (w, dto, reportNo) => "√";
                    if (layout.Contains("Lining") && !string.IsNullOrEmpty(layout)) map["AF4"] = (w, dto, reportNo) => "√";

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
                    // 2. 固定顺序的单元格列表
                    var cellOrder = new List<string>{
                        "A5", "A6", "A7", "A8", "A9", "A10","A11", "A12","A13","A14", "A15", "A16"
                    };
                    var selectedParts = (component ?? "")
                        .Split('-', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(k => descMap.ContainsKey(k))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // 4. 按顺序依次填，发完为止
                    for (int i = 0; i < selectedParts.Count && i < cellOrder.Count; i++)
                    {
                        string part = selectedParts[i];
                        string cell = cellOrder[i];
                        string desc = descMap[part];
                        map[cell] = (w, dto, reportNo) => desc;
                    }
                }
                return map;
            },
            ["Seam Strength"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["A3"] = (w, dto, reportNo) => dto.Standard!;
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    string? component = SeamExtraHelper.GetExtraField<string>(dto, "component", objIndex: 0);
                    string? layout = SeamExtraHelper.GetExtraField<string>(dto, "layout", objIndex: 0);

                    map["J18"] = (w, dto, reportNo) => dto.Standard!;
                    if (layout!.Contains("Shell") && !string.IsNullOrEmpty(layout)) map["Q19"] = (w, dto, reportNo) => "√";
                    if (layout.Contains("Lining") && !string.IsNullOrEmpty(layout)) map["AF19"] = (w, dto, reportNo) => "√";

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
                    // 2. 固定顺序的单元格列表
                    var cellOrder = new List<string>{
                        "A20", "A21", "A22", "A23", "A24", "A25","A26", "A27","A28","A29", "A30", "A31"
                    };
                    var selectedParts = (component ?? "")
                        .Split('-', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(k => descMap.ContainsKey(k))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // 4. 按顺序依次填，发完为止
                    for (int i = 0; i < selectedParts.Count && i < cellOrder.Count; i++)
                    {
                        string part = selectedParts[i];
                        string cell = cellOrder[i];
                        string desc = descMap[part];
                        map[cell] = (w, dto, reportNo) => desc;
                    }
                }
                return map;
            },
        };



        private static readonly Dictionary<string, int> OffsetRule = new()
        {
            ["Colour Fastness to Perspiration"] = 6,
            ["Dimensional Stability"] = 4,
            ["Stability to Washing"] = 4,
            ["Stability to Dry Cleaning"] = 4,
            ["Colour Fastness to Non Chlorine Bleach"] = 6,
            ["Shower Resistant Claims Spray Rating"] = 3,
            ["Absorbency of Textiles"] = 6,
            ["Waterproof Claims Hydrostatic Head"] = 2
        };
        private void WriteSamples(
            ExcelWorksheet ws,
            string[] slice,
            int[]? afmap,
            string[] cellAddrs,
            string[]? AfterWashCellAddrs,
            string itemName,
            string sampleDescription,
            string standard)
        {
            int offset = OffsetRule.GetValueOrDefault(itemName, 0);
            if ((itemName == "Dimensional Stability"||itemName == "Stability to Washing") && !sampleDescription.Contains("Fabric")) offset = 0;

            if (afmap != null && afmap.Length > 0 
                && AfterWashCellAddrs != null 
                && AfterWashCellAddrs.Length > 0 
                && itemName == "Appearance-Common"
                && standard  != "PM01")
            {
                for (int i = 0; i < AfterWashCellAddrs.Length; i++)
                {
                    ws.Cells[AfterWashCellAddrs![i]].Value = afmap[0];
                }
            }
            else if (afmap != null && afmap.Length > 0 
                && (itemName == "Stability to Washing" || itemName == "Dimensional Stability")
                && !sampleDescription.Contains("Fabric"))
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


            if (itemName == "Appearance"
                ||itemName== "Dimensional and Bra Wire Casing Stability"
                ||itemName == "Appearance-Common")
            {
                for (int i = 0; i < cellAddrs.Length; i++)
                {
                    ws.Cells[cellAddrs[i]].Value = slice[0];
                }
            }
            else if (itemName == "Colour Fastness to Hot Pressing")
            {
                for (int i = 0; i < slice.Length; i++)
                {
                    ws.Cells[cellAddrs[i]].Value = slice[i];
                    ws.Cells[cellAddrs[i + 3]].Value = slice[i];
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
