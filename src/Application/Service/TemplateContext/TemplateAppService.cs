using Mapster;
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
using System.Text.Json;

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
        public async Task<Result> CreateTemplateAsync(AddTemplateDto dto, CancellationToken ct)
        {
            if (dto.TemplateFile == null)
                return Result.Fail("Template file is required");

            // 扩展名取自上传文件本身
            var fileExtension = Path.GetExtension(dto.TemplateFile.FileName);

            using (var stream = dto.TemplateFile.OpenReadStream())
            {
                var validationResult = await _fileSecurityValidator.ValidateAsync(stream, fileExtension);

                if (!validationResult.IsValid)
                    return Result.Fail("Template is Unsafe");
            }

            // 只接受 .docx / .xlsx
            var fileType = MapFileExtensionToType(fileExtension);
            if (fileType == null)
                return Result.Fail($"不支持的文件类型: {fileExtension}（只接受 .docx / .xlsx）");

            var templateId = _templateIdGenerator.Generate(dto.TestType, dto.TemplateName);

            // 将 dto.Site (string) 解析为 Site 枚举
            if (!Enum.TryParse<Site>(dto.Site, true, out var site))
            {
                return Result.Fail($"无效的 Site: {dto.Site}");
            }

            // ==================== 解析三个 JSON 字符串 ====================
            Dictionary<string, object> templateIndexDict;
            List<TestConditionTextTemplateDto> conditionTextDtos;
            TemplateSturctureDto structureDto;

            try
            {
                templateIndexDict = string.IsNullOrWhiteSpace(dto.TemplateIndexJson)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(dto.TemplateIndexJson)
                      ?? new Dictionary<string, object>();

                conditionTextDtos = string.IsNullOrWhiteSpace(dto.TestConditionTextTemplateDtosJson)
                    ? new List<TestConditionTextTemplateDto>()
                    : JsonSerializer.Deserialize<List<TestConditionTextTemplateDto>>(dto.TestConditionTextTemplateDtosJson)
                      ?? new List<TestConditionTextTemplateDto>();

                structureDto = string.IsNullOrWhiteSpace(dto.TemplateSturctureDtoJson)
                    ? new TemplateSturctureDto()
                    : JsonSerializer.Deserialize<TemplateSturctureDto>(dto.TemplateSturctureDtoJson)
                      ?? new TemplateSturctureDto();
            }
            catch (JsonException ex)
            {
                return Result.Fail($"元数据 JSON 解析失败: {ex.Message}");
            }

            // ==================== 构造领域对象 ====================
            Template template;
            try
            {
                // TestConditionTextTemplate 列表
                var conditionTexts = new List<TestConditionTextTemplate>();
                foreach (var textTemplateDto in conditionTextDtos)
                {
                    // ★ 把 value 归一化后构造成 Dictionary<string, object>
                    var dict = textTemplateDto.TemplateIndex
                        .ToDictionary(kv => kv.Key, kv => NormalizeJsonElement(kv.Value) ?? (object)string.Empty);

                    var conditionText = TestConditionTextTemplate.Create(
                        dict,                    // ← 传 Dictionary
                        textTemplateDto.Text);

                    conditionTexts.Add(conditionText);
                }

                // TemplateIndex
                var templateIndex = TemplateIndex.Create(
                    templateIndexDict
                        .Select(kv => new KeyValuePair<string, object>(kv.Key, NormalizeJsonElement(kv.Value))));

                // TemplateStructure
                var templateStructure = TemplateStructure.Create(
                    structureDto.TestConditionCount,
                    structureDto.TestMethodCount,
                    structureDto.SampleDataAreaCount,
                    structureDto.SampleResultAreaCount,
                    structureDto.AfterWashDataCount,
                    templateId);

                template = Template.Create(
                    templateId,
                    dto.TemplateName,
                    site,
                    fileType.Value,
                    dto.Category,
                    templateIndex,
                    templateStructure,
                    conditionTexts);
            }
            catch (ArgumentException ex)
            {
                return Result.Fail(ex.Message);
            }

            // ==================== 落盘 + 持久化 ====================
            var url = template.GetTemplateUrl();

            await _fileStorageService.SaveFileFromStreamAsync(
                dto.TemplateFile.OpenReadStream(), url, url);

            await _templateRepository.AddAsync(template, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 将文件扩展名映射到 TemplateFileType 枚举。不支持的扩展名返回 null。
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

        /// <summary>
        /// 把 JsonSerializer 反序列化出的 JsonElement 还原成 CLR 基础类型，
        /// 避免 AreEqual 里因类型不匹配导致索引比对失败。
        /// </summary>
        private static object? NormalizeJsonElement(object? value)
        {
            if (value is JsonElement je)
            {
                return je.ValueKind switch
                {
                    JsonValueKind.String => je.GetString(),
                    JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDecimal(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => je.GetRawText()
                };
            }
            return value;
        }

    }
}
