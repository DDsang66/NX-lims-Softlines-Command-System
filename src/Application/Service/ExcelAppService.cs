using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;
using System.Text.RegularExpressions;

namespace NX_lims_Softlines_Command_System.src.Application.Service
{

    public class ExcelAppService : IScopedDependency
    {
        private readonly IWebHostEnvironment _env;
        private readonly IFileHashService _hashService;
        private readonly IServerConfig _serverConfig; // 注入接口
        private readonly IExcelAddressRepository _excelRepo; // 注入接口

        public ExcelAppService(IWebHostEnvironment env, IFileHashService hashService, IServerConfig serviceConfig, IExcelAddressRepository excelAddressRepository)
        {
            _env = env;
            _hashService = hashService;
            _serverConfig = serviceConfig;
            _excelRepo = excelAddressRepository;
        }

        /// <summary>
        /// 获取 Excel 文件的访问信息
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="buyer"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public async Task<Result<ExcelUrlResponseDto>> GetExcelAccessInfoAsync(string repo, string buyer, string group)
        {

            if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(buyer) || string.IsNullOrEmpty(group))
                return Result<ExcelUrlResponseDto>.Fail("参数不能为空");

            // 3. 定义正则表达式：匹配类似 "87.405.26..01" 这种包含 ".." 的不完整格式
            // 解释：\d+ 匹配数字，\. 匹配点，\.\. 匹配两个连续的点
            var isIncompleteRepo = Regex.IsMatch(repo, @"\.\.");

            string filePath;

            if (isIncompleteRepo)
            {
                var searchDir = Path.Combine(_env.WebRootPath, "ExcelModel");
                // 文件名规则：Buyer_Group_sheet.xlsx
                var fileName = $"{buyer}_{group.ToUpper()}_sheet.xlsx";

                filePath = Path.Combine(searchDir, fileName);
            }
            else
            {
                var searchDir = Path.Combine(_env.WebRootPath, "ExcelModel\\SavingExcel");

                var fileName = await _excelRepo.GetFilePathAsync(repo, buyer, group.ToUpper());

                if (string.IsNullOrWhiteSpace(fileName)) return Result<ExcelUrlResponseDto>.Fail("未找到匹配的 Excel 文件路径");

                filePath = Path.Combine(searchDir, fileName);
            }


            if (!File.Exists(filePath))
            {
                return Result<ExcelUrlResponseDto>.Fail($"文件不存在: {filePath}");
            }

            if (!File.Exists(filePath)) throw new FileNotFoundException("Excel file not found");

            var actualFileName = Path.GetFileName(filePath);

            var fileBytes = await File.ReadAllBytesAsync(filePath);

            var hashString = await _hashService.ComputeHashAsync(fileBytes);

            var fileKey = $"{repo}_{hashString}";

            var baseUrl = _serverConfig.GetBaseUrl();

            return Result<ExcelUrlResponseDto>.Ok(new ExcelUrlResponseDto
            {
                fileKey = fileKey,
                fileName = actualFileName,
                downloadUrl = $"{baseUrl}/api/worksheet/{repo}/{actualFileName}/download",
                callbackUrl = $"{baseUrl}/api/worksheet/{repo}/{actualFileName}/callback"
            });

        }



        /// <summary>
        /// 获取 Excel 文件的访问信息
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public string GetExcelFilePathAsync(string fileName,string repoNum)
        {
            var isIncompleteRepo = fileName.Contains(repoNum);
            string filePath;
            if (isIncompleteRepo)
            {
                filePath = Path.Combine(_env.WebRootPath, "ExcelModel\\SavingExcel", fileName);
            }
            else 
            {
                filePath = Path.Combine(_env.WebRootPath, "ExcelModel", fileName);
            }
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文件未找到: {filePath}");
            return filePath;
        }
    }
}
