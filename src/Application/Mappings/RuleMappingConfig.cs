using Mapster;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class RuleMappingConfig:IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            //领域模型=>数据库
            config.NewConfig<ParamRule, BasicParamRule>()
                .Map(dest => dest.RuleId, src => src.Id.Value)
                .Map(dest => dest.FormulaId, src => src.FormulaId == null ? null : src.FormulaId.Value)
                .Map(dest => dest.ParamStructureId, src => src.StructureId == null ? null : src.StructureId.Value)
                .Map(dest => dest.StandardFamilyCodeId, src => src.StandardFamilyId == null ? null : src.StandardFamilyId.Value)
                .Map(dest => dest.ParamName, src => src.ParamName)
                .Map(dest => dest.Priority, src => src.Priority)
                .Map(dest => dest.DefaultValue, src => src.Result.Value)
                .Map(dest => dest.StopOnMatch, src => src.StopOnMatch)
                .Map(dest => dest.IsActive, src => src.IsActive)
                .Map(dest => dest.ConditionPattern, src => JsonSerializer.Serialize(src.Pattern, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false // 作为单个字段存储，通常不需要缩进
                }));

            // ========== 数据库 => 领域模型（使用 Reconstitute 工厂方法）==========
            config.NewConfig<BasicParamRule, ParamRule>()
                .MapWith(src => ParamRule.Reconstitute(
                    new ParamRuleId(src.RuleId),
                    string.IsNullOrEmpty(src.FormulaId)
                    ? null
                    : new FormulaId(src.FormulaId),
                    string.IsNullOrEmpty(src.ParamStructureId) 
                    ? null 
                    : new ParamStructureId(src.ParamStructureId),
                    string.IsNullOrEmpty(src.StandardFamilyCodeId) 
                    ? null
                    : new StandardFamilyId(src.StandardFamilyCodeId),
                    src.ParamName,
                    src.Priority,
                    new ParamValue(src.DefaultValue, null),
                    src.StopOnMatch,
                    src.IsActive,
                    string.IsNullOrEmpty(src.ConditionPattern)
                    ? null
                    : JsonSerializer.Deserialize<ConditionPattern>(src.ConditionPattern, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                ));

        }
     }
}
