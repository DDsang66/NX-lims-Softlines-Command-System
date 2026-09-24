using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    /// <summary>
    /// ParamRule 聚合根的映射配置
    /// 
    /// 映射方向：
    /// 1. 领域模型 → 数据库实体（持久化）
    /// 2. 数据库实体 → 领域模型（重建）
    /// 3. 领域模型 → 响应 DTO（前端展示）
    /// </summary>
    public class RuleMappingConfig : IRegister
    {
        /// <summary>
        /// 统一的 JSON 序列化选项
        /// - 忽略 null 值（减小存储体积）
        /// - 枚举写为字符串（可读性强，避免枚举值变更导致数据损坏）
        /// </summary>
        private static readonly JsonSerializerOptions SerializeOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// 统一的 JSON 反序列化选项
        /// - 大小写不敏感（兼容历史数据）
        /// - 支持枚举字符串（如 "GreaterThanOrEqual" → ComparisonOperator.GreaterThanOrEqual）
        /// </summary>
        private static readonly JsonSerializerOptions DeserializeOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public void Register(TypeAdapterConfig config)
        {
            RegisterDomainToEntity(config);
            RegisterEntityToDomain(config);
            RegisterDomainToResponseDto(config);
        }

        #region 1. 领域模型 → 数据库实体

        /// <summary>
        /// ParamRule → BasicParamRule（持久化）
        /// </summary>
        private void RegisterDomainToEntity(TypeAdapterConfig config)
        {
            config.NewConfig<ParamRule, BasicParamRule>()
                .Map(dest => dest.RuleId, src => src.Id.Value)
                .Map(dest => dest.FormulaId, src => src.FormulaId == null ? null : src.FormulaId.Value)
                .Map(dest => dest.ParamStructureId, src => src.StructureId == null ? null : src.StructureId.Value)
                .Map(dest => dest.ParamName, src => src.ParamName)
                .Map(dest => dest.Priority, src => src.Priority)
                .Map(dest => dest.StopOnMatch, src => src.StopOnMatch)
                .Map(dest => dest.IsActive, src => src.IsActive)
                .Map(dest => dest.EngineLayer, src => (byte)src.EngineLayer)

                // 结果值：配置态的 Result.Value
                // 注意：对于纯赋值模式，Result.Value 可能为 null，此时存 null
                .Map(dest => dest.DefaultValue, src => src.Result == null ? null : src.Result.Value)

                // 条件模式：序列化为 JSON（含 AssignMatches）
                .Map(dest => dest.ConditionPattern, src =>
                    src.Pattern == null
                        ? null
                        : JsonSerializer.Serialize(src.Pattern, SerializeOptions));
        }

        #endregion

        #region 2. 数据库实体 → 领域模型

        /// <summary>
        /// BasicParamRule → ParamRule（重建）
        /// 
        /// 使用 Reconstitute 工厂方法，不校验业务规则
        /// </summary>
        private void RegisterEntityToDomain(TypeAdapterConfig config)
        {
            config.NewConfig<BasicParamRule, ParamRule>()
                .MapWith(src => ParamRule.Reconstitute(
                    new ParamRuleId(src.RuleId),

                    // FormulaId
                    string.IsNullOrEmpty(src.FormulaId)
                        ? null
                        : new FormulaId(src.FormulaId),

                    // StructureId
                    string.IsNullOrEmpty(src.ParamStructureId)
                        ? null
                        : new ParamStructureId(src.ParamStructureId),

                    src.ParamName,
                    src.Priority,

                    // Result：从 DefaultValue 重建
                    new ParamValue(src.DefaultValue, null),

                    src.StopOnMatch,
                    src.IsActive,

                    // ConditionPattern：从 JSON 反序列化
                    string.IsNullOrEmpty(src.ConditionPattern)
                        ? new ConditionPattern()  // 空 JSON → 空 Pattern（避免下游 NRE）
                        : JsonSerializer.Deserialize<ConditionPattern>(src.ConditionPattern, DeserializeOptions)
                          ?? new ConditionPattern(),

                    // EngineLayer：null 时回退到 Standard
                    (EngineLayer)(src.EngineLayer ?? (byte)EngineLayer.Standard)
                ));
        }

        #endregion

        #region 3. 领域模型 → 响应 DTO

        /// <summary>
        /// ParamRule → ParamRuleResponseDto（前端展示）
        /// 
        /// 特点：
        /// - Pattern 的嵌套结构展开为 DTO 顶层字段
        /// - 支持全部 5 种匹配模式
        /// - Composite 支持递归 Children
        /// </summary>
        private void RegisterDomainToResponseDto(TypeAdapterConfig config)
        {
            config.NewConfig<ParamRule, ParamRuleResponseDto>()
                .Map(dest => dest.Id, src => src.Id.Value)
                .Map(dest => dest.FormulaId, src => src.FormulaId == null ? null : src.FormulaId.Value)
                .Map(dest => dest.ParamStructureId, src => src.StructureId == null ? null : src.StructureId.Value)
                .Map(dest => dest.ParamName, src => src.ParamName)
                .Map(dest => dest.Priority, src => src.Priority)
                .Map(dest => dest.IsActive, src => src.IsActive)
                .Map(dest => dest.StopOnMatch, src => src.StopOnMatch)

                // 结果
                .Map(dest => dest.ResultValue, src => src.Result == null ? null : src.Result.Value)
                .Map(dest => dest.ResultNotes, src => src.Result == null ? null : src.Result.Notes)

                // 匹配模式（Pattern 为 null 时返回空集合，避免下游 NRE）
                .Map(dest => dest.EqualMatches, src =>
                    src.Pattern == null
                        ? new List<EqualMatchDto>()
                        : src.Pattern.EqualMatches
                            .Select(kv => new EqualMatchDto { Field = kv.Key, Value = kv.Value })
                            .ToList())

                .Map(dest => dest.ComparisonMatches, src =>
                    src.Pattern == null
                        ? new List<ComparisonMatchDto>()
                        : src.Pattern.ComparisonMatches
                            .Select(c => new ComparisonMatchDto
                            {
                                FieldPath = c.FieldPath,
                                Operator = c.Operator.ToString(),
                                ExpectedValue = c.ExpectedValue
                            })
                            .ToList())

                .Map(dest => dest.InMatches, src =>
                    src.Pattern == null
                        ? new List<InMatchDto>()
                        : src.Pattern.InMatches
                            .Select(kv => new InMatchDto
                            {
                                Field = kv.Key,
                                Values = kv.Value!.Select(v => v!).ToList()
                            })
                            .ToList())

                // Composite：支持递归 Children
                .Map(dest => dest.CompositeMatches, src =>
                    src.Pattern == null
                        ? new List<CompositeConditionDto>()
                        : src.Pattern.CompositeMatches
                            .Select(MapCompositeToDto)
                            .ToList())

                // AssignMatches（新增）
                .Map(dest => dest.AssignMatches, src =>
                    src.Pattern == null
                        ? new List<AssignMatchDto>()
                        : src.Pattern.AssignMatches
                            .Select(a => new AssignMatchDto
                            {
                                SourceFieldPath = a.SourceFieldPath,
                                IsRequired = a.IsRequired,
                                DefaultValue = a.DefaultValue
                            })
                            .ToList());
        }

        /// <summary>
        /// CompositeCondition → CompositeConditionDto（递归）
        /// </summary>
        private static CompositeConditionDto MapCompositeToDto(CompositeCondition src)
        {
            if (src == null)
                return new CompositeConditionDto();

            return new CompositeConditionDto
            {
                Logic = src.Logic.ToString(),
                FieldNames = src.FieldNames?.ToList() ?? new List<string>(),

                SubConditions = src.SubConditions?
                    .Select(sc => new ComparisonMatchDto
                    {
                        FieldPath = sc.FieldPath,
                        Operator = sc.Operator.ToString(),
                        ExpectedValue = sc.ExpectedValue
                    })
                    .ToList() ?? new List<ComparisonMatchDto>(),

                // 递归映射 Children
                Children = src.Children?
                    .Select(MapCompositeToDto)
                    .ToList() ?? new List<CompositeConditionDto>()
            };
        }

        #endregion
    }
}