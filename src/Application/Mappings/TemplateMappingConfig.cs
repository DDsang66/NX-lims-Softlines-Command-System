using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using Template = NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.Template;  
namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class TemplateMappingConfig: IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // ==========================================
            // 1. 聚合根 -> PO (用于持久化到数据库)
            // ==========================================
            config.NewConfig<Template, src.Infrastructure.Data.Persistence.Template>()
                // 映射主键：聚合根的 Id (TemplateId) -> PO 的 TemplateId (string)
                .Map(dest => dest.Id, src => src.Id.Value)

                // 映射枚举到 byte
                .Map(dest => dest.Site, src => (byte)src.Site)
                .Map(dest => dest.Status, src => (byte)src.Status)
                .Map(dest => dest.FileType, src => (byte)src.FileType);


            //config.NewConfig<src.Infrastructure.Data.Persistence.Template, Template>()
            //    .ConstructUsing(src => Template.Rebuild(
            //        new TemplateId(src.Id),                 // 1. id
            //        src.TemplateName,                       // 2. templateName   
            //        src.TemplateUrl,                        // 3. templateUrl
            //        (Site)src.Site,                         // 4. site
            //        (Status)src.Status,                     // 5. status 
            //        (TemplateFileType)src.FileType,         // 6. fileType
            //        src.BusinessCategory,                   // 7. string (如 BusinessCategory)
            //        src.TemplateIndex,                      // 8. templateIndex  
            //        src.TemplateStructure,                  // 9. templateStructure
            //        src.Version,                            // 10. version 
            //        src.UpdateAt,                           // 11. updateAt
            //        src.TestConditionTextTemplates ))
            //    // 忽略自动映射，因为 ConstructUsing 已经完整构建了对象
            //    .Ignore(dest => dest.Id)
            //    .Ignore(dest => dest.TemplateName)
            //    .Ignore(dest => dest.TemplateUrl)
            //    .Ignore(dest => dest.Site)
            //    .Ignore(dest => dest.Status)
            //    .Ignore(dest => dest.Version)
            //    .Ignore(dest => dest.UpdateAt)
            //    .Ignore(dest => dest.FileType)
            //    .Ignore(dest => dest.BusinessCategory);

            TypeAdapterConfig<Template, TemplateResponseDto>
                .NewConfig()
                // 主键映射：聚合根的 Id -> DTO 的 TemplateId
                .Map(dest => dest.TemplateId, src => src.Id.Value)
                .Map(dest => dest.BusinessCategory, src => src.BusinessCategory)
                // 枚举映射：转换为字符串
                .Map(dest => dest.Site, src => src.Site.ToString())
                .Map(dest => dest.Status, src => src.Status.ToString())
                .Map(dest => dest.FileType, src => src.FileType.ToString())
                // 普通字段直接映射（名字一样，Mapster 默认能映射，但显式写更清晰）
                .Map(dest => dest.TemplateName, src => src.TemplateName)
                .Map(dest => dest.TemplateUrl, src => src.TemplateUrl)
                .Map(dest => dest.Version, src => src.Version)
                .Map(dest => dest.UpdateAt, src => src.UpdateAt)
                // ★ 新增字段
                .Map(dest => dest.TemplateIndex,
                     src => src.TemplateIndex != null
                         ? src.TemplateIndex.Values.ToDictionary(kv => kv.Key, kv => kv.Value)
                         : new Dictionary<string, object>())
                .Map(dest => dest.TestConditionTextTemplates,
                     src => src.TestConditionTextTemplates.Select(x => new TestConditionTextTemplateResponseDto
                     {
                         TemplateIndex = x.TemplateIndex != null
                             ? x.TemplateIndex.Values.ToDictionary(kv => kv.Key, kv => kv.Value)
                             : new Dictionary<string, object>(),
                         Text = x.Text
                     }).ToList())
                .Map(dest => dest.TemplateStructure,
                     src => src.TemplateStructure != null
                         ? new TemplateStructureResponseDto
                         {
                             TestConditionCount = src.TemplateStructure.TestConditionCount,
                             TestMethodCount = src.TemplateStructure.TestMethodCount,
                             SampleDataAreaCount = src.TemplateStructure.SampleDataAreaCount,
                             SampleResultAreaCount = src.TemplateStructure.SampleResultAreaCount,
                             AfterWashDataCount = src.TemplateStructure.AfterWashDataCount
                         }
                         : null);
        }
    }
}
