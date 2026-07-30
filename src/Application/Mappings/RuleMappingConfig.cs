using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext;
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

            // ========== 领域模型 => 响应 DTO（Pattern 嵌套 → DTO 顶层）==========
            config.NewConfig<ParamRule, ParamRuleResponseDto>()
                .Map(dest => dest.Id, src => src.Id.Value)
                .Map(dest => dest.FormulaId, src => src.FormulaId == null ? null : src.FormulaId.Value)
                .Map(dest => dest.ParamStructureId, src => src.StructureId == null ? null : src.StructureId.Value)
                .Map(dest => dest.ParamName, src => src.ParamName)
                .Map(dest => dest.Priority, src => src.Priority)
                .Map(dest => dest.IsActive, src => src.IsActive)
                .Map(dest => dest.ResultValue, src => src.Result.Value)
                .Map(dest => dest.ResultNotes, src => src.Result.Notes)
                .Map(dest => dest.EqualMatches, src => src.Pattern == null ? null! :
                    src.Pattern.EqualMatches.Select(kv => new EqualMatchDto { Field = kv.Key, Value = kv.Value! }).ToList())
                .Map(dest => dest.ComparisonMatches, src => src.Pattern == null ? null! :
                    src.Pattern.ComparisonMatches.Select(c => new ComparisonMatchDto
                    { FieldPath = c.FieldPath, Operator = c.Operator.ToString(), ExpectedValue = c.ExpectedValue! }).ToList())
                .Map(dest => dest.InMatches, src => src.Pattern == null ? null! :
                    src.Pattern.InMatches.Select(kv => new InMatchDto { Field = kv.Key, Values = kv.Value!.Select(v => v!).ToList() }).ToList())
                .Map(dest => dest.CompositeMatches, src => src.Pattern == null ? null! :
                    src.Pattern.CompositeMatches.Select(c => new CompositeConditionDto
                    {
                        Logic = c.Logic.ToString(),
                        FieldNames = c.FieldNames!,
                        SubConditions = c.SubConditions == null ? null! :
                            c.SubConditions.Select(sc => new ComparisonMatchDto
                            { FieldPath = sc.FieldPath, Operator = sc.Operator.ToString(), ExpectedValue = sc.ExpectedValue! }).ToList()!
                    }).ToList());

        }
     }
}
