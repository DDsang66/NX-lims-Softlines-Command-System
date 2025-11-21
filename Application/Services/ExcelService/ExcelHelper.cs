using OfficeOpenXml;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService
{
    public sealed class ExcelHelper
    {
        private readonly IPrintExcelStrategyFactory _factory;

        public ExcelHelper(IPrintExcelStrategyFactory factory)
        {
            _factory = factory;
        }
        /// <summary>
        /// 根据三套模板路径与三套输出路径，把 JSON 数据一次性填充并保存
        /// </summary>
        public async Task FillExcelAsync(
            string templatePathWet,
            string templatePathPhysics,
            string excelOutputPathWet,
            string excelOutputPathPhy,
            ExcelSubmitDto Dto, IPrintExcelStrategyFactory _factory)          // 用 object 代替 dynamic，性能更好
        {
            try
            {
                // 异步复制模板
                await Task.WhenAll(
                    CopyAsync(templatePathWet, excelOutputPathWet),
                    CopyAsync(templatePathPhysics, excelOutputPathPhy)
                );

                //异步加载包
                var packageWet = await LoadAsync(excelOutputPathWet);
                var packagePhy = await LoadAsync(excelOutputPathPhy);

                // 取买家策略
                var strategy = _factory.GetStrategy(Dto.Buyer!);
                strategy.PrintJsonData(Dto, packageWet, packagePhy);

                // 5. 异步保存
                await Task.WhenAll(
                    SaveAsync(packageWet),
                    SaveAsync(packagePhy)
                );
            }
            catch (Exception ex)
            {
                // 可替换为 ILogger
                Console.WriteLine($"FillExcelAsync Error: {ex}");
            }
        }


        #region
        ///// <summary>
        ///// 内存版：把模板填完后写回 MemoryStream（零磁盘，除 1 个临时文件）
        ///// </summary>
        //public async Task FillExcelAsync(
        //        string templatePathWet,
        //        string templatePathPhysics,
        //        MemoryStream outputWetMs,
        //        MemoryStream outputPhyMs,
        //        ExcelSubmitDto Dto,
        //        IPrintExcelStrategyFactory factory)
        //{
        //    var tempDir = Path.GetTempPath();          // 本地临时目录，速度最快
        //    var wetTmp = Path.Combine(tempDir, $"wet_{Guid.NewGuid()}.xlsx");
        //    var phyTmp = Path.Combine(tempDir, $"phy_{Guid.NewGuid()}.xlsx");

        //    try
        //    {
        //        // ① 先走原有“文件路径”逻辑
        //        await FillExcelAsync(templatePathWet, templatePathPhysics,
        //                             wetTmp, phyTmp, Dto, factory);

        //        // ② 把填好的文件读进 MemoryStream
        //        using (var fs = File.OpenRead(wetTmp)) await fs.CopyToAsync(outputWetMs);
        //        using (var fs = File.OpenRead(phyTmp)) await fs.CopyToAsync(outputPhyMs);

        //        // ③ 复位指针，方便上层直接压缩
        //        outputWetMs.Position = 0;
        //        outputPhyMs.Position = 0;
        //    }
        //    finally
        //    {
        //        // ④ 清理临时文件
        //        if (File.Exists(wetTmp)) File.Delete(wetTmp);
        //        if (File.Exists(phyTmp)) File.Delete(phyTmp);
        //    }
        //}
        #endregion





        /* ---------- 私有辅助 ---------- */
        private static Task CopyAsync(string src, string dst)
        {
            return Task.Run(() =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                File.Copy(src, dst, true);
            });
        }

        private static Task<ExcelPackage> LoadAsync(string path)
        {
            return Task.Run(() => new ExcelPackage(new FileInfo(path)));
        }

        private static Task SaveAsync(ExcelPackage package)
        {
            return Task.Run(() => package.Save());
        }

        public static void CompareAndMarkChanges(string templateFilePath, string targetFilePath)
        {
            // 加载模板文件
            using (var templatePackage = new ExcelPackage(new FileInfo(templateFilePath)))
            {
                // 加载目标文件
                using (var targetPackage = new ExcelPackage(new FileInfo(targetFilePath)))
                {
                    // 获取模板文件和目标文件的工作表
                    var templateWorksheets = templatePackage.Workbook.Worksheets;
                    var targetWorksheets = targetPackage.Workbook.Worksheets;

                    // 遍历每个工作表
                    foreach (var templateWorksheet in templateWorksheets)
                    {
                        string templateSheetName = templateWorksheet.Name;

                        var matchingTargetWorksheets = targetWorksheets
                            .Where(ws => ws.Name.StartsWith(templateSheetName))
                            .ToList();

                        foreach (var targetWorksheet in matchingTargetWorksheets)
                        {
                            foreach (var cell in templateWorksheet.Cells)
                            {
                                var targetCell = targetWorksheet.Cells[cell.Start.Row, cell.Start.Column];

                                if (!Equals(cell.Value, targetCell.Value))
                                {
                                    targetCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    targetCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                                }

                                if (cell.Style.Fill.BackgroundColor.Rgb != targetCell.Style.Fill.BackgroundColor.Rgb ||
                                    cell.Style.Font.Bold != targetCell.Style.Font.Bold ||
                                    cell.Style.Font.Italic != targetCell.Style.Font.Italic ||
                                    cell.Style.Font.UnderLine != targetCell.Style.Font.UnderLine ||
                                    cell.Style.Font.Strike != targetCell.Style.Font.Strike)
                                {
                                    targetCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    targetCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                                }
                            }
                        }
                    }

                    // 保存目标文件
                    targetPackage.Save();
                }
            }
        }
    }
}

