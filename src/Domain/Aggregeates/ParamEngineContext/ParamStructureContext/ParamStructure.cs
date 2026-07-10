using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext
{
    public sealed class ParamStructure : IAggregateRoot
    {
        public ParamStructureId Id { get; private set; }
        public StandardFamilyId? FamilyId { get; private set; }  // 关联标准族
        public FormulaId? FormulaId { get; private set; }       // 引用 Formula 聚合
        public string ParamName { get; private set; } = string.Empty;  // 例如 "Ballast"
        public ParamSchema Schema { get; private set; } 
        public List<ParamRuleId> ApplicableRuleIds { get; private set; } = new();
        public DateTime EffectiveDate { get; private set; }

        //状态

        private ParamStructure() { }

        /// <summary>
        /// 工厂：创建单参数结构，保证 Schema 至少包含一项主参数定义
        /// </summary>
        public static ParamStructure Create(
            ParamStructureId id,
            StandardFamilyId? familyId,
            FormulaId? formulaId,
            string paramName,
            ParamSchema schema,
            IEnumerable<ParamRuleId>? ruleIds,
            DateTime? effectiveDate = null)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(paramName)) throw new ArgumentException("paramName required", nameof(paramName));
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (schema.RequiredParam == null)
                throw new ArgumentException("Schema must contain at least one ParamDefinition", nameof(schema));

            var ps = new ParamStructure
            {
                Id = id,
                FamilyId = familyId,
                FormulaId = formulaId,
                ParamName = paramName.Trim(),
                Schema = schema,
                ApplicableRuleIds = ruleIds?.ToList() ?? new List<ParamRuleId>(),
                EffectiveDate = effectiveDate ?? DateTime.UtcNow
            };

            return ps;
        }

        /// <summary>
        /// 主参数定义（Schema.RequiredParam）
        /// </summary>
        public ParamDefinition MainParamDefinition => Schema.RequiredParam;

        /// <summary>
        /// 验证二级条件池是否满足结构要求（结构层面）
        /// - 只做字段存在性、白名单基础校验
        /// - 更复杂的语义校验（表达式、数据类型细化）由 Formula/语义分析器完成
        /// </summary>
        public Result ValidateConditionPool(ConditionPool pool)
        {
            if (pool == null) return Result.Fail("ConditionPool is null");

            foreach (var requirement in Schema.ConditionRequirements)
            {
                if (requirement.IsRequired && !pool.HasCondition(requirement.FieldName))
                    return Result.Fail($"Missing required condition: {requirement.FieldName}");

                if (pool.HasCondition(requirement.FieldName) && requirement.AllowedValues != null && requirement.AllowedValues.Any())
                {
                    var value = pool.GetConditionValue<object>(requirement.FieldName);
                    if (!requirement.AllowedValues.Contains(value))
                        return Result.Fail($"Condition '{requirement.FieldName}' has invalid value");
                }
            }

            return Result.Ok();
        }

        /// <summary>
        /// 接受来自引擎的初步生成结果，并执行本聚合的策略性处理（主要针对主参数）
        /// - 责任：本地补偿策略、记录、局部越界检查（更复杂的全局补偿应由 ParamCompensationService 完成）
        /// - 返回：最终的 ParamSet 供应用层持久化或进一步处理
        /// </summary>
        public ParamSet AcceptGeneratedResult(ParamSet generated)
        {
            if (generated == null) throw new ArgumentNullException(nameof(generated));

            var result = new ParamSet();
            var main = MainParamDefinition;
            var name = main.Name;

            if (generated.TryGetValue(name, out var value))
            {
                // 简单本地越界检查（若 Schema.Limitations 提供 IsValid 会使用）
                if (Schema.Limitations != null && Schema.Limitations.TryGetValue(name, out var limitation))
                {
                    try
                    {
                        // 如果 limitation 未实现 IsValid，也允许通过（以兼容占位）
                        var ok = limitation?.GetType().GetMethod("IsValid")?.Invoke(limitation, new[] { value }) as bool?;
                        if (ok == false)
                            throw new Exception($"{name} has invalid value: {value}");
                    }
                    catch (Exception) { throw; }
                    catch
                    {
                        // 忽略 limitation 反射异常，允许继续（后续由 ParamCompensationService 进行严格校验）
                    }
                }

                result.Add(name, value);
            }
            else
            {
                // 补偿：使用默认值
                result.Add(name, main.DefaultValue);
            }

            return result;
        }

        /// <summary>
        /// 更新生效日期
        /// </summary>
        /// <param name="effective"></param>
        public void UpdateEffectiveDate(DateTime effective) => EffectiveDate = effective;

        /// <summary>
        /// 添加适用规则
        /// </summary>
        /// <param name="ruleId"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddRule(ParamRuleId ruleId)
        {
            if (ruleId == null) throw new ArgumentNullException(nameof(ruleId));
            if (!ApplicableRuleIds.Contains(ruleId)) ApplicableRuleIds.Add(ruleId);
        }

        /// <summary>
        /// 移除适用规则
        /// </summary>
        /// <param name="ruleId"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void RemoveRule(ParamRuleId ruleId)
        {
            if (ruleId == null) throw new ArgumentNullException(nameof(ruleId));
            ApplicableRuleIds.Remove(ruleId);
        }
    }
}
