using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service;
using NX_lims_Softlines_Command_System.src.Domain.Services;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using System.Reflection.Emit;

namespace NX_lims_Softlines_Command_System.src.Application.Service.TemplateContext
{
    public class TemplateAppService : IScopedDependency,ITemplateAppService
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IServerConfig _serverConfig;
        private readonly IFileSecurityValidator _fileSecurityValidator;
        private readonly IFileStorageService _fileStorageService;
        private readonly ITemplateIdGenerator _templateIdGenerator;

        public TemplateAppService(
            ITemplateRepository templateRepository,
            IUnitOfWork unitOfWork,
            IServerConfig serverConfig,
            IFileSecurityValidator fileSecurityValidator,
            IFileStorageService fileStorageService,
            ITemplateIdGenerator templateIdGenerator)
        {
            _templateRepository = templateRepository;
            _unitOfWork = unitOfWork;
            _serverConfig = serverConfig;
            _fileSecurityValidator = fileSecurityValidator;
            _fileStorageService = fileStorageService;
            _templateIdGenerator = templateIdGenerator;
        }

        /// <summary>
        /// 创建模板
        /// </summary>
        /// <param name="dto">模板数据传输对象</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        public async Task<Result> CreateTemplateAsync(AddTemplateDto dto, CancellationToken ct) 
        {
            if (dto.TemplateFile == null)
                return Result.Fail("Template file is required");

            using (var stream = dto.TemplateFile.OpenReadStream())
            {
                // ✅ 从文件名提取扩展名
                var fileExtension = Path.GetExtension(dto.TemplateFile.FileName);

                var validationResult = await _fileSecurityValidator.ValidateAsync(stream, fileExtension);

                if (!validationResult.IsValid)
                    return Result.Fail("Template is Unsafe");
            }

            var host = _serverConfig.GetBaseUrl();

            var templateId = _templateIdGenerator.Generate(dto.TestType, dto.TemplateName);

            // 将 dto.Site (string) 解析为 Site 枚举
            if (!Enum.TryParse<Site>(dto.Site, true, out var site))
            {
                return Result.Fail($"无效的 Site: {dto.Site}");
            }

            // 将 dto.FileType (string) 解析为 TemplateFileType 枚举
            var fileType = MapFileExtensionToType(dto.FileType);

            var template = Template.Create(templateId, dto.TemplateName, site, fileType, host,dto.Category);

            var url = template.GetTemplateUrl();

            await _fileStorageService.SaveFileFromStreamAsync(dto.TemplateFile.OpenReadStream(), url, url);

            await _templateRepository.AddAsync(template, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        // 将文件扩展名映射到 TemplateFileType 枚举
        private TemplateFileType MapFileExtensionToType(string fileExtension)
        {
            return fileExtension?.ToLower() switch
            {
                ".docx" or ".doc" => TemplateFileType.Docx,
                ".xlsx" or ".xls" or ".xlsm" => TemplateFileType.Excel,
                _ => throw new ArgumentException($"Unsupported file type: {fileExtension}")
            };
        }
    }
}
