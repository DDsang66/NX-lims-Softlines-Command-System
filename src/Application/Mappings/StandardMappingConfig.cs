using Mapster;
using NX_lims_Softlines_Command_System.Domain.Aggregeates.Standard;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class StandardMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Standard, BasicStandard>()
                   .Map(dest => dest.IdStandard, src => src.Id.Value)
                   .Map(dest => dest.StandardCode, src => src.StandardCode)
                   .Map(dest => dest.StandardCodeNameEn, src => src.StandardCodeNameEn)
                   .Map(dest => dest.StandardCodeNameChn, src => src.StandardCodeNameChn)
                   .Map(dest => dest.Status, src => (byte)src.Status)
                   .Map(dest => dest.StandardFamilyCodeId, src => src.StandardFamilyCode == null ? null : src.StandardFamilyCode.Value);

            // ========== 数据库 => 领域模型 ==========
            config.NewConfig<BasicStandard, Standard>()
                .MapWith(src => Standard.Reconstitute(
                    new StandardId(src.IdStandard),
                    src.StandardCode,
                    src.StandardCodeNameEn,
                    src.StandardCodeNameChn,
                    (Status)src.Status,
                    string.IsNullOrEmpty(src.StandardFamilyCodeId) 
                    ? null 
                    : new StandardFamilyId(src.StandardFamilyCodeId)
                ));


            // ========== 领域模型 => API响应 DTO (StandardResponseDto) ==========
            config.NewConfig<Standard, StandardResponseDto>()
                // 假设 DTO 的主键是 string 类型，提取 Value
                .Map(dest => dest.idstandard, src => src.Id.Value)

                // 名称和编码等直接按名称自动映射（如果字段名完全一致，这几行其实可以省略，
                // Mapster 默认按同名映射，但显式写出来可读性更好，便于后续维护）
                .Map(dest => dest.StandardCode, src => src.StandardCode)
                .Map(dest => dest.StandardCodeNameChn, src => src.StandardCodeNameChn)
                .Map(dest => dest.StandardCodeNameEn, src => src.StandardCodeNameEn)

                // ✅ 状态转字符串：将枚举/值对象转为字符串供前端使用
                .Map(dest => dest.Status, src => src.Status.ToString())
                // ✅ 简化空值判断：如果 StandardFamilyCode 为 null，?.Value 直接返回 null
                // 前提：DTO 中对应的字段类型必须是 string (或可空类型 string?)
                .Map(dest => dest.StandardFamilyCode, src => src.StandardFamilyCode == null ? null : src.StandardFamilyCode.Value);
        }
    }
}
