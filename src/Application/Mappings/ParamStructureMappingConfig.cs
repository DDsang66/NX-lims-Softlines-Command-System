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


            // ==========================================
            // 新增配置：聚合根 → ParamStructureResponseDto (反向转化)
            // ==========================================
            config.NewConfig<ParamStructure, ParamStructureResponseDto>()
                // 基础属性提取
                .Map(dest => dest.ParamStructureId, src => src.Id.Value)
                .Map(dest => dest.ParamName, src => src.ParamName)
                .Map(dest => dest.FormulaId, src => src.FormulaId.Value)
                .Map(dest => dest.EffectiveDate, src => src.EffectiveDate)

                // 集合属性提取并转化为 List<Guid>
                .Map(dest => dest.StandardFamilyIds,
                     src => src.StandardFamilyIds != null
                            ? src.StandardFamilyIds.Select(id => id.Value).ToList()
                            : new List<string>())
                .Map(dest => dest.RuleIds,
                     src => src.ApplicableRuleIds != null
                            ? src.ApplicableRuleIds.Select(id => id.Value).ToList()
                            : new List<string>())

                // 将 ParamSchema 值对象映射回 SchemaDto
                .Map(dest => dest.ParamSchema, src => src.Schema.Adapt<SchemaDto>());


            // 领域值对象 ParamSchema → SchemaDto (反向映射)
            config.NewConfig<ParamSchema, SchemaDto>()
                .Map(dest => dest.RequiredParam, src => src.RequiredParam.Adapt<ParamDefinitionDto>())
                .Map(dest => dest.ConditionRequirements, src => src.ConditionRequirements.Select(c => c.Adapt<ConditionRequirementDto>()).ToList())
                .Map(dest => dest.Limitations, src => src.Limitations.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Adapt<ParamLimitationDto>()
                    ));

            // 领域值对象 ParamDefinition → ParamDefinitionDto (反向映射)
            config.NewConfig<ParamDefinition, ParamDefinitionDto>()
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.ValueType, src => src.ValueTypeName) // 注意反向映射字段名对应
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.IsNullable, src => src.IsNullable)
                .Map(dest => dest.DefaultValue, src => src.DefaultValue);

            // 领域值对象 ConditionRequirement → ConditionRequirementDto (反向映射)
            config.NewConfig<ConditionRequirement, ConditionRequirementDto>()
                .Map(dest => dest.FieldName, src => src.FieldName)
                .Map(dest => dest.FieldType, src => src.ValueTypeName) // 注意反向映射字段名对应
                .Map(dest => dest.IsRequired, src => src.IsRequired)
                .Map(dest => dest.AllowedValues, src => src.AllowedValues);

            // 领域值对象 ParamLimitation → ParamLimitationDto (反向映射)
            config.NewConfig<ParamLimitation, ParamLimitationDto>()
                .Map(dest => dest.ValueType, src => src.ValueTypeName) // 注意反向映射字段名对应
                .Map(dest => dest.AllowedValues, src => src.AllowedValues)
                .Map(dest => dest.Min, src => src.Min)
                .Map(dest => dest.Max, src => src.Max);

            // ==========================================
            // 补充：数据库模型 → 聚合根 ParamStructure (如果查询时需要)
            // ==========================================
            // 如果你的查询逻辑是通过 EF Core 读出 BasicParamStructure 再转为聚合根，可以配置这段
            /*
            config.NewConfig<BasicParamStructure, ParamStructure>()
                .MapWith(src => ParamStructure.Create(
                    new ParamStructureId(src.ParamStructureId),
                    new List<StandardFamilyId>(), // 数据库表若未存储关联，则默认空集合
                    new FormulaId(Guid.Empty),    // 视数据库结构而定
                    src.ParamName,
                    DeserializeSchema(src.Schema), // 反序列化 JSON
                    new List<ParamRuleId>(),
                    src.EffectiveDate
                ));
            */
        }

        /// <summary>
        /// 辅助方法：将数据库中的 JSON 字符串反序列化为 ParamSchema 值对象
        /// </summary>
        private static ParamSchema DeserializeSchema(string schemaJson)
        {
            if (string.IsNullOrWhiteSpace(schemaJson))
                return default; // 视 ParamSchema.Create 逻辑而定，可能需要返回默认值或抛异常

            var schemaDto = JsonSerializer.Deserialize<SchemaDto>(schemaJson, 
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return schemaDto.Adapt<ParamSchema>();
        }
    }
}
