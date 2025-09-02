using System.Diagnostics;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;

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
            if (_env.IsDevelopment())
            {
                foreach (string path in paths)
                {
                    SelectPrintExcel.ProcessExcelFile(path);//删除空白工作单
                    if (!File.Exists(path))
                        continue;
                    //Process.Start(new ProcessStartInfo(path)
                    //{
                    //    UseShellExecute = true
                    //});

                }
            }
            var existingPaths = new[] { wetOut, phyOut }.Where(File.Exists).ToList();

            if (!existingPaths.Any())
                throw new FileNotFoundException("未生成任何 Excel 文件，请检查模板或数据。");

            return (existingPaths.FirstOrDefault(p => p == wetOut),
                    existingPaths.FirstOrDefault(p => p == phyOut));

        }
    }
}
