using Mapster;
using NX_lims_Softlines_Command_System.Domain.Aggregeates.Standard;
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
        }
    }
}
