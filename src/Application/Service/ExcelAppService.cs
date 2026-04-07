using Azure.Core;
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
        private readonly IServerConfig _serverConfig; 
        private readonly IExcelAddressRepository _excelRepo; 

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

            // 定义正则表达式：匹配类似 "87.405.26..01" 这种包含 ".." 的不完整格式
            // \d+ 匹配数字，\. 匹配点，\.\. 匹配两个连续的点
            // demo中暂定为无单号则传出模板文件路径，后续根据实际情况调整
            var filePath = await GetExcelFilePathAsync(repo, buyer, group);

            if (!File.Exists(filePath) || string.IsNullOrEmpty(filePath))  return Result<ExcelUrlResponseDto>.Fail($"文件不存在或未找到匹配路径: {filePath}");
           
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
        /// 保存前端修改的Excel文件
        /// </summary>
        /// <param name="saveAsRequest"></param>
        /// <returns></returns>
        public async Task<Result> SaveAsExcelAccessInfoAsync(SaveAsRequest saveAsRequest) 
        {
            // 从 OnlyOffice 的临时 URL 下载文件
            using var httpClient = new HttpClient();

            var fileBytes = await httpClient.GetByteArrayAsync(saveAsRequest.fileUrl);

            var filePath = await GetExcelFilePathAsync(saveAsRequest.reportNum, saveAsRequest.buyer, saveAsRequest.group);

            if (string.IsNullOrEmpty(filePath)) return Result.Fail("未找到匹配的 Excel 文件路径");

            await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);

            // 更新数据库（根据业务逻辑）
            // await _db.SaveDocument(request.reportNum, savePath, request.group, request.buyer);

            return Result.Ok();
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

        /// <summary>
        ///  获取Excel文件路径
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="buyer"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        private async Task<string> GetExcelFilePathAsync(string repo, string buyer, string group)
        {
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

                if (string.IsNullOrWhiteSpace(fileName))
                    return null;

                filePath = Path.Combine(searchDir, fileName);
            }

            return filePath;
        }

    }
}
