using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class ConditionMappingConfig:IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            //dto=>entity
            config.NewConfig<AddConditionPoolDto, ConditionPool>()
                .MapWith(src => ConditionPool.Create(
                    new CheckListId(src.CheckListId),
                    new Dictionary<string, object?>()
                    ));

            //聚合根=>数据库模型
           config.NewConfig<ConditionPool, src.Infrastructure.Data.Persistence.ConditionPool>()
                .Map(dest => dest.ConditionPoolId, src => src.Id.Value)
                .Map(dest => dest.CheckListId, src => src.CheckListId.Value)
                .Map(dest => dest.Conditions, src => src.Conditions)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                .Map(dest => dest.Status, src => src.Status);
        }
    }
}
