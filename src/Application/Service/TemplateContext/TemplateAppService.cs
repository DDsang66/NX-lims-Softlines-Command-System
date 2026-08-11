using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Application.Service.TemplateContext
{
    public class TemplateAppService : IScopedDependency,ITemplateAppService
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IServerConfig _serverConfig;
        private readonly IFileSecurityValidator _fileSecurityValidator;
        private readonly IFileStorageService _fileStorageService;

        public TemplateAppService(
            ITemplateRepository templateRepository,
            IUnitOfWork unitOfWork,
            IServerConfig serverConfig,
            IFileSecurityValidator fileSecurityValidator,
            IFileStorageService fileStorageService)
        {
            _templateRepository = templateRepository;
            _unitOfWork = unitOfWork;
            _serverConfig = serverConfig;
            _fileSecurityValidator = fileSecurityValidator;
            _fileStorageService = fileStorageService;
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
                var validationResult = await _fileSecurityValidator.ValidateAsync(stream, dto.FileType);

                if (!validationResult.IsValid)
                    return Result.Fail("Template is Unsafe");
            }

            var host = _serverConfig.GetBaseUrl();

            var templateId = new TemplateId(Guid.NewGuid().ToString());

            // 将 dto.Site (string) 解析为 Site 枚举
            if (!Enum.TryParse<Site>(dto.Site, true, out var site))
            {
                return Result.Fail($"无效的 Site: {dto.Site}");
            }

            // 将 dto.FileType (string) 解析为 TemplateFileType 枚举
            if (!Enum.TryParse<TemplateFileType>(dto.FileType, true, out var fileType))
            {
                return Result.Fail($"无效的 FileType: {dto.FileType}");
            }

            var template = Template.Create(templateId, dto.TemplateName, site, fileType, host,dto.Category);

            var url = template.GetTemplateUrl();

            await _fileStorageService.SaveFileFromStreamAsync(dto.TemplateFile.OpenReadStream(), url, url);

            await _templateRepository.AddAsync(template, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}
