using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Application.Interface.TemplateContext;
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
        private readonly IFileSecurityValidator _fileSecurityValidator;
        private readonly IFileStorageService _fileStorageService;
        private readonly ITemplateIdGenerator _templateIdGenerator;

        public TemplateAppService(
            ITemplateRepository templateRepository,
            IUnitOfWork unitOfWork,
            IFileSecurityValidator fileSecurityValidator,
            IFileStorageService fileStorageService,
            ITemplateIdGenerator templateIdGenerator)
        {
            _templateRepository = templateRepository;
            _unitOfWork = unitOfWork;
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

            // 扩展名取自上传文件本身 —— dto.FileType 是业务分类字符串(如 "Docx"), 不是扩展名
            var fileExtension = Path.GetExtension(dto.TemplateFile.FileName);

            using (var stream = dto.TemplateFile.OpenReadStream())
            {
                var validationResult = await _fileSecurityValidator.ValidateAsync(stream, fileExtension);

                if (!validationResult.IsValid)
                    return Result.Fail("Template is Unsafe");
            }

            // 生成的 URL 与落盘文件名都按 FileType 固定取 .docx / .xlsx,
            // 放 .doc / .xls / .xlsm 进来会得到"叫 .docx 但内容是旧格式"的文件, 这里直接拒掉。
            var fileType = MapFileExtensionToType(fileExtension);
            if (fileType == null)
                return Result.Fail($"不支持的文件类型: {fileExtension}（只接受 .docx / .xlsx）");

            var templateId = _templateIdGenerator.Generate(dto.TestType, dto.TemplateName);

            // 将 dto.Site (string) 解析为 Site 枚举
            if (!Enum.TryParse<Site>(dto.Site, true, out var site))
            {
                return Result.Fail($"无效的 Site: {dto.Site}");
            }

            Template template;
            try
            {
                template = Template.Create(
                    templateId, dto.TemplateName, site, fileType.Value, dto.Category, null, null, null);
            }
            catch (ArgumentException ex)
            {
                // 模板名 / 业务分类会被拼进 URL 路径, 净化失败是调用方输入问题,
                // 回可读的 Fail 而不是让异常直穿成 500
                return Result.Fail(ex.Message);
            }

            var url = template.GetTemplateUrl();

            await _fileStorageService.SaveFileFromStreamAsync(dto.TemplateFile.OpenReadStream(), url, url);

            await _templateRepository.AddAsync(template, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 将文件扩展名映射到 TemplateFileType 枚举。不支持的扩展名返回 null (调用方回可读的失败)。
        /// 只认 .docx / .xlsx —— 生成的文件名会按 FileType 硬编码这两个扩展名。
        /// </summary>
        private static TemplateFileType? MapFileExtensionToType(string? fileExtension)
        {
            return fileExtension?.ToLowerInvariant() switch
            {
                ".docx" => TemplateFileType.Docx,
                ".xlsx" => TemplateFileType.Excel,
                _ => null
            };
        }
    }
}
