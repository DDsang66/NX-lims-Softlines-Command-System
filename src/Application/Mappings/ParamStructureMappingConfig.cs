using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class ParamStructureMappingConfig:IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // DTO → 领域模型
            config.NewConfig<AddParamStructureDto, ParamStructure>()
                .MapWith(src => ParamStructure.Create(
                    new ParamStructureId(src.ParamStructureId),
                    src.StandardFamilyIds == null
                    ? new List<StandardFamilyId>()
                    : src.StandardFamilyIds.Select(id => new StandardFamilyId(id)).ToList(),
                    new FormulaId(src.FormulaId),
                    src.ParamName,
                    src.ParamSchema.Adapt<ParamSchema>(),  // Mapster 递归映射
                    src.RuleIds == null
                    ? new List<ParamRuleId>()
                    : src.RuleIds.Select(id => new ParamRuleId(id)).ToList(),
                    src.EffectiveDate
                ));

            // SchemaDto → ParamSchema
            config.NewConfig<SchemaDto, ParamSchema>()
                .MapWith(src => ParamSchema.Create(
                    src.RequiredParam.Adapt<ParamDefinition>(),
                    src.ConditionRequirements.Select(c => c.Adapt<ConditionRequirement>()),
                    src.Limitations.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Adapt<ParamLimitation>()
                    )
                ));

            // ParamDefinitionDto → ParamDefinition
            config.NewConfig<ParamDefinitionDto, ParamDefinition>()
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.ValueTypeName, src =>
                string.IsNullOrEmpty(src.ValueType)
                ? "System.String"
                : src.ValueType)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.IsNullable, src => src.IsNullable)
                .Map(dest => dest.DefaultValue, src => src.DefaultValue);

            // ConditionRequirementDto → ConditionRequirement
            config.NewConfig<ConditionRequirementDto, ConditionRequirement>()
                .Map(dest => dest.FieldName, src => src.FieldName)
                .Map(dest => dest.ValueTypeName, src =>
                string.IsNullOrEmpty(src.FieldType)
                ? "System.String"
                : src.FieldType)
                .Map(dest => dest.IsRequired, src => src.IsRequired)
                .Map(dest => dest.AllowedValues, src => src.AllowedValues);

            // ParamLimitationDto → ParamLimitation
            config.NewConfig<ParamLimitationDto, ParamLimitation>()
                .Map(dest => dest.ValueTypeName, src =>
                string.IsNullOrEmpty(src.ValueType)
                ? "System.String"
                : src.ValueType)
                .Map(dest => dest.AllowedValues, src => src.AllowedValues)
                .Map(dest => dest.Min, src => src.Min)
                .Map(dest => dest.Max, src => src.Max);

            // 领域模型 → 数据库模型
            config.NewConfig<ParamStructure, BasicParamStructure>()
                .Map(dest => dest.ParamStructureId, src => src.Id.Value)
                .Map(dest => dest.ParamName, src => src.ParamName)
                .Map(dest => dest.Schema, 
                src => JsonSerializer.Serialize(
                    src.Schema,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))  // Schema 序列化为 JSON
                .Map(dest => dest.EffectiveDate, src => src.EffectiveDate);

            //// 数据库 → 领域模型
            //config.NewConfig<BasicParamStructure, ParamStructure>()
            //    .MapWith(src => ParamStructure.Reconstitute(
            //        new ParamStructureId(src.ParamStructureId),
            //        new StandardFamilyId(src.StandardFamilyCodeId),
            //        new FormulaId(src.FormulaId),
            //        src.ParamName,
            //        DeserializeSchema(src.SchemaJson),
            //        DeserializeRuleIds(src.ApplicableRuleIdsJson),
            //        src.EffectiveDate
            //    ));
        }
    }
}
