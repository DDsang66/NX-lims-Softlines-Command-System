using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamFormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class FormulaMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            //转化为数据库模型
            config.NewConfig<Formula, BasicFormula>()
                .Map(dest => dest.FormulaId, src => src.Id.Value)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.ParamName, src => src.ParamName)
                .Map(dest => dest.ConditionFields, src => JsonSerializer.Serialize(src.ConditionFields, (JsonSerializerOptions?)null))
                .Map(dest => dest.ExpressionTemplate, src => src.ExpressionTemplate)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.Version, src => src.Version)
                .Map(dest => dest.IsActive, src => src.IsActive)
                .Map(dest => dest.EngineLayer, src => src.EngineLayer)
                .Map(dest => dest.EffectiveDate, src => src.EffectiveDate);

            //转化为领域模型,用开放的领域方法重建
            //config.NewConfig<BasicFormula, Formula>()
            //    .MapWith(src => Formula.Reconstitute(
            //        new FormulaId(src.FormulaId),
            //        src.Name,
            //        src.ParamName,
            //        JsonSerializer.Deserialize<List<string>>(src.ConditionFields!, (JsonSerializerOptions?)null) ?? new List<string>(),
            //        src.ExpressionTemplate!,
            //        src.Version??0,
            //        src.IsActive,
            //        src.EffectiveDate,
            //        src.Description
            //    ));

            //聚合根=>dto
            config.NewConfig<Formula, FormulaResponseDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.ParamStrurctureIds, src => src.ParamStructureIds.Select(ps => ps.Value).ToList())
            .Map(dest => dest.StandardFamilyIds, src => src.StandardFamilyIds.Select(sf => sf.Value).ToList())
            .Map(dest => dest.BuyerCodes, src => src.BuyerIds.Select(bc => bc.Value).ToList())
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.ParamName, src => src.ParamName)
            .Map(dest => dest.ConditionFields, src => src.ConditionFields)
            .Map(dest => dest.ExpressionTemplate, src => src.ExpressionTemplate)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.Version, src => src.Version)
            .Map(dest => dest.EffectiveDate, src => src.EffectiveDate)
            .Map(dest => dest.EngineLayer, src => src.EngineLayer)
            .Map(dest => dest.IsActive, src => src.IsActive);
        }
     }
}
