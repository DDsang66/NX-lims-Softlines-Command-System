using Mapster;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class StandardFamilyMappingConfig:IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            //领域模型->数据库模型
            config.NewConfig<StandardFamily, BasicStandardFamily>()
                .Map(dest => dest.IdStandardFamily, src => src.Id.Value)
                .Map(dest => dest.StandardFamilyCode, src => src.StandardFamilyCode)
                .Map(dest => dest.Version, src => src.Version)
                .Map(dest => dest.EffectiveDate, src => src.EffectiveDate);

            //数据库模型->领域模型
            //config.NewConfig<BasicStandardFamily, StandardFamily>()
            //    .ConstructUsing((BasicStandardFamily src, global::Mapster.ITypeAdapterContext ctx) => StandardFamily.Reconstitute(
            //        new StandardFamilyId(src.IdStandardFamily),
            //        src.StandardFamilyCode,
            //        ctx.GetParameter<List<StandardId>>("StandardIds") ?? new List<StandardId>(),
            //        ctx.GetParameter<List<FormulaId>>("FormulaIds") ?? new List<FormulaId>(),
            //        ctx.GetParameter<List<ParamStructureId>>("ParamStructureIds") ?? new List<ParamStructureId>(),
            //        ctx.GetParameter<List<ParamRuleId>>("SharedRuleIds") ?? new List<ParamRuleId>(),
            //        src.Version,
            //        src.EffectiveDate
            //    ));
        }
     }
}
