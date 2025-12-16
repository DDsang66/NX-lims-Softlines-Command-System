using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelPrintTool;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using OfficeOpenXml;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService
{
    public class ReceiveDataHelper
    {
        private readonly IWebHostEnvironment _env;
        private readonly ExcelHelper _excel;
        private readonly IPrintExcelStrategyFactory _factory;
        public ReceiveDataHelper(ExcelHelper excel, IWebHostEnvironment env, IPrintExcelStrategyFactory factory)
        {
            _excel = excel;
            _env = env;
            _factory = factory;
        }
        public async Task<(string? wetOut, string? phyOut)> Helper(ExcelSubmitDto dto)
        {
            var outDir = Path.Combine(_env.WebRootPath, "ExcelModel/SavingExcel");
            Directory.CreateDirectory(outDir);

            string wetTemplate = Path.Combine(_env.WebRootPath, GetPath.GetTemplatePath(dto, "WET"));//复制路径计算
            string wetOut = Path.Combine(outDir, GetPath.GetOutputPath(dto, "WET"));
            string physicsTemplate = Path.Combine(_env.WebRootPath, GetPath.GetTemplatePath(dto, "PHY"));//复制路径计算
            string phyOut = Path.Combine(outDir, GetPath.GetOutputPath(dto, "PHY"));//输出路径计算


            await _excel.FillExcelAsync(
                wetTemplate, physicsTemplate,
                wetOut, phyOut,
                dto, _factory);

            ExcelHelper.CompareAndMarkChanges(wetTemplate, wetOut);
            ExcelHelper.CompareAndMarkChanges(physicsTemplate, phyOut);
            // 本机打开（开发环境可选）
            var paths = new List<string> { wetOut, phyOut };

            foreach (string path in paths)
            {
                SelectPrintExcel.ProcessExcelFile(path);//删除空白工作单
                if (!File.Exists(path))
                    continue;

            }
            var existingPaths = new[] { wetOut, phyOut }.Where(File.Exists).ToList();

            if (!existingPaths.Any())
                throw new Exception("未生成任何 Excel 文件，请检查模板或数据。");

            return (existingPaths.FirstOrDefault(p => p == wetOut),
                    existingPaths.FirstOrDefault(p => p == phyOut));

        }
    }
    #region
    //public class ReceiveDataHelper
    //{
    //    private readonly IWebHostEnvironment _env;
    //    private readonly ExcelHelper _excel;
    //    private readonly IPrintExcelStrategyFactory _factory;

    //    public ReceiveDataHelper(ExcelHelper excel, IWebHostEnvironment env, IPrintExcelStrategyFactory factory)
    //    {
    //        _env = env;
    //        _excel = excel;
    //        _factory = factory;
    //    }

    //    /// <summary>
    //    /// 0 磁盘写、0 临时文件，返回两份 Excel 的内存流
    //    /// </summary>
    //    public async Task<(MemoryStream? wetMs, MemoryStream? phyMs)> GenerateAsync(ExcelSubmitDto dto)
    //    {
    //        // 1. 模板路径计算（复用原逻辑）
    //        string wetTemplate = Path.Combine(_env.WebRootPath, GetPath.GetTemplatePath(dto, "WET"));
    //        string physicsTemplate = Path.Combine(_env.WebRootPath, GetPath.GetTemplatePath(dto, "PHY"));

    //        // 2. 内存流承载结果
    //        var wetMs = new MemoryStream();
    //        var phyMs = new MemoryStream();

    //        // 3. 填充（要求 ExcelHelper 新增 Stream 重载，见下方提示）
    //        await _excel.FillExcelAsync(
    //            wetTemplate, physicsTemplate,
    //            wetMs, phyMs,        // 原来是 wetOut/phyOut 路径，现在直接给 Stream
    //            dto, _factory);

    //        // 4. 对比标记（内存版）
    //        await MarkChangesAsync(wetTemplate, wetMs);
    //        await MarkChangesAsync(physicsTemplate, phyMs);

    //        // 5. 删除空白工作表（内存版）
    //        await CleanEmptySheetsAsync(wetMs);
    //        await CleanEmptySheetsAsync(phyMs);

    //        // 6. 如果某一份根本没数据，把流置空
    //        if (wetMs.Length == 0) { wetMs.Dispose(); wetMs = null; }
    //        if (phyMs.Length == 0) { phyMs.Dispose(); phyMs = null; }

    //        return (wetMs, phyMs);
    //    }

    //    /*-------- 下面 3 个私有方法把原来的“磁盘文件”操作改成 Stream 版，逻辑完全不变 --------*/
    //    private async Task MarkChangesAsync(string templatePath, MemoryStream resultMs)
    //    {
    //        if (resultMs == null || resultMs.Length == 0) return;

    //        resultMs.Position = 0;
    //        using var pkg = new ExcelPackage(resultMs); // EPPlus 直接读内存
    //        /* 这里照搬 ExcelHelper.CompareAndMarkChanges 里的代码，
    //           只是把 new FileInfo(...) 换成 resultMs 即可 */
    //        // …… 你的标记逻辑 ……
    //        // 加载模板文件
    //        using (var templatePackage = new ExcelPackage(resultMs))
    //        {
    //            // 加载目标文件
    //            using (var targetPackage = new ExcelPackage(resultMs))
    //            {
    //                // 获取模板文件和目标文件的工作表
    //                var templateWorksheets = templatePackage.Workbook.Worksheets;
    //                var targetWorksheets = targetPackage.Workbook.Worksheets;

    //                // 遍历每个工作表
    //                foreach (var templateWorksheet in templateWorksheets)
    //                {
    //                    string templateSheetName = templateWorksheet.Name;

    //                    var matchingTargetWorksheets = targetWorksheets
    //                        .Where(ws => ws.Name.StartsWith(templateSheetName))
    //                        .ToList();

    //                    foreach (var targetWorksheet in matchingTargetWorksheets)
    //                    {
    //                        foreach (var cell in templateWorksheet.Cells)
    //                        {
    //                            var targetCell = targetWorksheet.Cells[cell.Start.Row, cell.Start.Column];

    //                            if (!Equals(cell.Value, targetCell.Value))
    //                            {
    //                                targetCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
    //                                targetCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
    //                            }

    //                            if (cell.Style.Fill.BackgroundColor.Rgb != targetCell.Style.Fill.BackgroundColor.Rgb ||
    //                                cell.Style.Font.Bold != targetCell.Style.Font.Bold ||
    //                                cell.Style.Font.Italic != targetCell.Style.Font.Italic ||
    //                                cell.Style.Font.UnderLine != targetCell.Style.Font.UnderLine ||
    //                                cell.Style.Font.Strike != targetCell.Style.Font.Strike)
    //                            {
    //                                targetCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
    //                                targetCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
    //                            }
    //                        }
    //                    }
    //                }

    //                // 保存目标文件
    //                targetPackage.Save();
    //            }
    //        }


    //        pkg.Save();   // 保存仍写回同一个 MemoryStream
    //    }

    //    private async Task CleanEmptySheetsAsync(MemoryStream ms)
    //    {
    //        if (ms == null || ms.Length == 0) return;
    //        ms.Position = 0;
    //        using var pkg = new ExcelPackage(ms);
    //        var empty = pkg.Workbook.Worksheets
    //                       .Where(w => w.Dimension?.Address == null ||
    //                                   w.Cells.Sum(c => c.Value == null ? 0 : 1) == 0)
    //                       .ToList();
    //        empty.ForEach(w => pkg.Workbook.Worksheets.Delete(w));
    //        pkg.Save();
    //    }
    //}

     #endregion

}
