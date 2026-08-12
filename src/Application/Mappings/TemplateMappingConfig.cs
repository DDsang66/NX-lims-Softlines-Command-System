using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext;
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
                .Map(dest => dest.TemplateId, src => src.Id.Value)

                // 映射枚举到 byte
                .Map(dest => dest.Site, src => (byte)src.Site)
                .Map(dest => dest.Status, src => (byte)src.Status)
                .Map(dest => dest.FileType, src => (byte)src.FileType);


            config.NewConfig<src.Infrastructure.Data.Persistence.Template, Template>()
                // ❌ 错误写法：表达式树中不能使用命名参数 (id: src.TemplateId)
                // ✅ 正确写法：严格按照 Rebuild 方法的参数顺序传入
                .ConstructUsing(src => Template.Rebuild(
                    new TemplateId(src.TemplateId), // 1. id
                    src.TemplateName,               // 2. templateName
                    src.TemplateUrl,                // 3. templateUrl
                    (Site)src.Site,                 // 4. site
                    (Status)src.Status,             // 5. status
                    src.Version,                    // 6. version
                    src.UpdateAt                    // 7. updateAt
                ))
                // 忽略自动映射，因为 ConstructUsing 已经完整构建了对象
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.TemplateName)
                .Ignore(dest => dest.TemplateUrl)
                .Ignore(dest => dest.Site)
                .Ignore(dest => dest.Status)
                .Ignore(dest => dest.Version)
                .Ignore(dest => dest.UpdateAt)
                .Ignore(dest => dest.FileType)
                .Ignore(dest => dest.BusinessCategory);

            config.NewConfig<Template, TemplateResponseDto>()
               // 主键映射：聚合根的 Id -> DTO 的 TemplateId
               .Map(dest => dest.TemplateId, src => src.Id.Value)
               .Map(dest => dest.BusinessCategory, src => src.BusinessCategory)
               // 枚举映射：转换为字符串
               .Map(dest => dest.Site, src => src.Site.ToString())
               .Map(dest => dest.Status, src => src.Status.ToString())
               .Map(dest => dest.FileType, src => src.FileType.ToString());

        }
    }
}
