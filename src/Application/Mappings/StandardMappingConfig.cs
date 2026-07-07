using Mapster;
using NX_lims_Softlines_Command_System.Domain.Aggregeates.Standard;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class StandardMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // 将 Standard 领域模型映射到 BasicStandard PO
            config.NewConfig<Standard, BasicStandard>()
                .Map(dest => dest.IdStandard, src => src.IdStandard.Value)
                .Map(dest => dest.StandardCode, src => src.StandardCode)
                .Map(dest => dest.StandardCodeNameEn, src => src.StandardCodeNameEn)
                .Map(dest => dest.StandardCodeNameChn, src => src.StandardCodeNameChn)
                .Map(dest => dest.Status, src => (byte)src.Status)
                .Map(dest => dest.StandardFamilyCodeId, src => src.StandardFamilyCode.Value);

            // 反向映射（例如，从数据库加载时）
            config.NewConfig<BasicStandard, Standard>()
                .Map(dest => dest.IdStandard, src => new StandardId(src.IdStandard))
                .Map(dest => dest.StandardCode, src => src.StandardCode)
                .Map(dest => dest.StandardCodeNameEn, src => src.StandardCodeNameEn)
                .Map(dest => dest.StandardCodeNameChn, src => src.StandardCodeNameChn)
                .Map(dest => dest.Status, src => (Status)src.Status)
                .Map(dest => dest.StandardFamilyCode, src => new StandardFamilyId(src.StandardFamilyCodeId));
        }
    }
}
