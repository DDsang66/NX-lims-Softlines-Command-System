using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelPrintTool;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Domain.Model;

using OfficeOpenXml;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService
{
    public class ReceiveDataHelper
    {
        private readonly IWebHostEnvironment _env;
        private readonly ExcelHelper _excel;
        private readonly IPrintExcelStrategyFactory _factory;
        private LabDbContextSec _labDbContextSec;
        public ReceiveDataHelper(ExcelHelper excel, IWebHostEnvironment env, IPrintExcelStrategyFactory factory,LabDbContextSec labDbContextSec)
        {
            _excel = excel;
            _env = env;
            _factory = factory;
            _labDbContextSec = labDbContextSec;
        }
        public async Task<(string? wetOut, string? phyOut)> Helper(ExcelSubmitDto dto)
        {
            var outDir = Path.Combine(_env.WebRootPath, "ExcelModel/SavingExcel");
            Directory.CreateDirectory(outDir);

            var wetPath = GetPath.GetTemplatePath(dto, "WET");
            var phyPath = GetPath.GetTemplatePath(dto, "PHY");
            var wetOutPath = GetPath.GetOutputPath(dto, "WET");
            var phyOutPath = GetPath.GetOutputPath(dto, "PHY");

            string wetTemplate = Path.Combine(_env.WebRootPath, wetPath);//复制路径计算
            string wetOut = Path.Combine(outDir, wetOutPath);
            string physicsTemplate = Path.Combine(_env.WebRootPath, phyPath);//复制路径计算
            string phyOut = Path.Combine(outDir, phyOutPath);//输出路径计算


            await _excel.FillExcelAsync(
                wetTemplate, physicsTemplate,
                wetOut, phyOut,
                dto, _factory);

            // 比较并标记更改
            ExcelHelper.CompareAndMarkChanges(wetTemplate, wetOut);
            ExcelHelper.CompareAndMarkChanges(physicsTemplate, phyOut);

            // 排 wetOut 文件
            SheetSorter.SortSheetsAndFixNames(wetOut);
            // 排 phyOut 文件
            SheetSorter.SortSheetsAndFixNames(phyOut);

            // 本机打开（开发环境可选）
            var paths = new List<string> { wetOut, phyOut };

            foreach (string path in paths)
            {
                SelectPrintExcel.ProcessExcelFile(path);//删除空白工作单
                if (!File.Exists(path))
                    continue;

            }
            var existingPaths = new[] { wetOut, phyOut }.Where(File.Exists).ToList();

            SaveExcelAddressesToDb(dto.ReportNumber, wetOutPath, phyOutPath);

            if (!existingPaths.Any())
                throw new Exception("未生成任何 Excel 文件，请检查模板或数据。");

            return (existingPaths.FirstOrDefault(p => p == wetOut),
                    existingPaths.FirstOrDefault(p => p == phyOut));

        }


        private void SaveExcelAddressesToDb(string reportNumber, string wetPath, string phyPath)
        {
            // 0. (可选) 先删除该报告号下的旧记录，防止数据重复

            var snowflake = new SnowflakeIdGenerator();
            // 1. 检查 Wet 文件是否存在，存在则添加记录
            if (!string.IsNullOrEmpty(wetPath))
            {
                _labDbContextSec.ExcelAddresses.Add(new Domain.Model.Entities.ExcelAddress
                {
                    IdExcelAddress = snowflake.NextId(),
                    ReportNumber = reportNumber,
                    Status = "Active",
                    Address = wetPath
                    // CreatedAt 会自动使用默认值
                });
            }

            // 2. 检查 Phy 文件是否存在，存在则添加记录
            if (!string.IsNullOrEmpty(phyPath))
            {
                _labDbContextSec.ExcelAddresses.Add(new Domain.Model.Entities.ExcelAddress
                {
                    IdExcelAddress = snowflake.NextId(),
                    ReportNumber = reportNumber,
                    Status = "Active",
                    Address = phyPath
                });
            }

            // 3. 统一保存更改
            // 注意：如果是在循环中调用，建议移到循环外保存，这里只保存一次
            _labDbContextSec.SaveChanges();
        }

    }

}
