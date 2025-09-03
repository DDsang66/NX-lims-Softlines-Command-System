using NX_lims_Softlines_Command_System.Application.DTO;
using OfficeOpenXml;
using static NX_lims_Softlines_Command_System.Tools.Factory.PrintExcelStrategyFactory;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.PrintExcelMethod
{
    public class PrintJakoExcel:IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintJakoExcel(LabDbContextSec db)
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

            ["CF to Sublimation in Storage"] = "CFtoSublimation&Ironing",
            ["CF to Hot Pressing"] = "CFtoSublimation&Ironing",

            ["CF to Sea Water"] = "CFtoCl&Sea&Yellow",
            ["CF to Chlorinated Water"] = "CFtoCl&Sea&Yellow",
            ["Phenolic Yellowing"] = "CFtoCl&Sea&Yellow",

            ["Print Durability For JAKO"] = "Print Durability",
            ["Heat Press Test For JAKO"] = "Heat Pressing Test",
            ["Spriality/Skewing"] = "Spirality",

            ["Weight"] = "Weight",
            ["Pilling Resistance"] = "Pilling Resistance",
            ["Snagging Resistance"] = "Abrasion&Snagging",
            ["Abrasion Resistance"] = "Abrasion&Snagging",
            ["Tensile Strength"] = "Seam Slippage&Tensile",
            ["Tear Strength"] = "Tear Strength",
            ["Extension and Recovery"] = "Stretch&Recovery of Elastic",
            ["Water Repellency-Spray Test"] = "WaterRepellency",
            ["Water Resistance-Hydrostatic Pressure"] = "Hydroatatic Test",

        };
        private static readonly Dictionary<string, Dictionary<string, string>> TemplateSheetNames = new()
        {
            ["Appearance"] = new Dictionary<string, string>
            {
                {"Fabric", "AppearanceAfterWashing-F" },
                {"Garment","AppearanceAfterWashing-G"},
            },
            ["DS to Dry-clean"] = new Dictionary<string, string>
            {
                {"Fabric", "DStoDryclean-F" },
                {"Garment", "DStoDryclean-G" },
                {"Socks", "DStoDryclean-Acc" },
                {"Gloves", "DStoDryclean-Acc" },
                {"Cap", "DStoDryclean-Acc" },
            },
            ["Seam Slippage"] = new Dictionary<string, string>
            {
                {"Fabric", "Seam Slippage&Tensile" },
                {"Garment", "Seam Slippage&Breakage" },
            },
            ["Zipper Strength"] = new Dictionary<string, string>
            {
                 {"EN","Zipper Strength-ASTM D2061"},
                 {"ASTM","Zipper Strength-EN 16732"}
            }
        };
        private static readonly Dictionary<string, Func<string, string, string[]>> CellMapper = new()
        {
            ["Appearance"] = (_, m) => ExcelJakoMapper.MapAppearance(m),
            ["DS to Dry-clean"] = (_, m) => ExcelJakoMapper.MapDStoDS(m),
            ["CF to Washing"] = (n, _) => ExcelJakoMapper.MapWRL(n),
            ["CF to Rubbing"] = (n, _) => ExcelJakoMapper.MapWRL(n),
            ["CF to Light"] = (n, _) => ExcelJakoMapper.MapWRL(n),
            ["CF to Perspiration"] = (n, _) => ExcelJakoMapper.MapPWD(n),
            ["CF to Water"] = (n, _) => ExcelJakoMapper.MapPWD(n),
            ["CF to Dry-clean"] = (n, _) => ExcelJakoMapper.MapPWD(n),
            ["Spriality/Skewing"] = (_, m) => ExcelJakoMapper.MapSpirality(m),
            ["Weight"] = (_, _) => ExcelJakoMapper.WeightMap(),
            ["Pilling Resistance"] = (_, _) => ExcelJakoMapper.PillingMap(),
            ["Seam Slippage"] = (n, _) => ExcelJakoMapper.STMap(n),
        };

        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, string, Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>>> WetExtraMap = new()
        { };
        private static readonly Dictionary<string, Func<CheckListDto, string, Dictionary<string, Func<CheckListDto, string, string>>>> PhyExtraMap = new()
        { };



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
