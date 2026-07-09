using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
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
                    string.IsNullOrEmpty(src.StandardFamilyId)
                    ? null
                    : new StandardFamilyId(src.StandardFamilyId),
                    string.IsNullOrEmpty(src.FormulaId)
                    ? null
                    : new FormulaId(src.FormulaId),
                    src.ParamName,
                    src.ParamSchema.Adapt<ParamSchema>(),  // Mapster 递归映射
                    null,  // ApplicableRuleIds 默认空
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
                .Map(dest => dest.ValueType, src => src.ValueType ?? typeof(string))
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.IsNullable, src => src.IsNullable)
                .Map(dest => dest.DefaultValue, src => src.DefaultValue);

            // ConditionRequirementDto → ConditionRequirement
            config.NewConfig<ConditionRequirementDto, ConditionRequirement>()
                .Map(dest => dest.FieldName, src => src.FieldName)
                .Map(dest => dest.FieldType, src => src.FieldType ?? typeof(string))
                .Map(dest => dest.IsRequired, src => src.IsRequired)
                .Map(dest => dest.AllowedValues, src => src.AllowedValues);

            // ParamLimitationDto → ParamLimitation
            config.NewConfig<ParamLimitationDto, ParamLimitation>()
                .Map(dest => dest.ValueType, src => src.ValueType ?? typeof(string))
                .Map(dest => dest.AllowedValues, src => src.AllowedValues)
                .Map(dest => dest.Min, src => src.Min)
                .Map(dest => dest.Max, src => src.Max);

            //// 领域模型 → 数据库实体（反向）
            //config.NewConfig<ParamStructure, BasicParamStructure>()
            //    .Map(dest => dest.ParamStructureId, src => src.Id.Value)
            //    .Map(dest => dest.StandardFamilyCodeId, src => src.FamilyId.Value)
            //    .Map(dest => dest.FormulaId, src => src.FormulaId.Value)
            //    .Map(dest => dest.ParamName, src => src.ParamName)
            //    .Map(dest => dest.SchemaJson, src => JsonSerializer.Serialize(src.Schema))  // Schema 序列化为 JSON
            //    .Map(dest => dest.EffectiveDate, src => src.EffectiveDate)
            //    .Map(dest => dest.ApplicableRuleIdsJson, src => JsonSerializer.Serialize(src.ApplicableRuleIds.Select(id => id.Value)));

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

        //private static ParamSchema DeserializeSchema(string? json)
        //{
        //    if (string.IsNullOrEmpty(json)) return ParamSchema.Create(ParamDefinition.Create("default", typeof(string), "", false, null));
        //    return JsonSerializer.Deserialize<ParamSchema>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        //}

        //private static List<ParamRuleId> DeserializeRuleIds(string? json)
        //{
        //    if (string.IsNullOrEmpty(json)) return new List<ParamRuleId>();
        //    var ids = JsonSerializer.Deserialize<List<string>>(json);
        //    return ids?.Select(id => new ParamRuleId(id)).ToList() ?? new List<ParamRuleId>();
        //}
    }
}
