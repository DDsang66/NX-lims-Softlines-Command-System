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
        /// <summary>
        /// 参数结构ID
        /// </summary>
        public ParamStructureId Id { get; private set; }

        private readonly List<StandardFamilyId?> _standardFamilyIds = new();
        private readonly List<ParamRuleId> _ruleIds  = new();
        private readonly List<FormulaId?> _formulaIds = new();

        /// <summary>
        /// 适用标准族
        /// </summary>
        public IReadOnlyCollection<StandardFamilyId?> StandardFamilyIds => _standardFamilyIds.AsReadOnly();

        /// <summary>
        /// 适用公式
        /// </summary>
        public IReadOnlyCollection<FormulaId?> FormulaIds => _formulaIds.AsReadOnly();
       
        /// <summary>
        /// 适用规则
        /// </summary>
        public IReadOnlyCollection<ParamRuleId> ApplicableRuleIds => _ruleIds.AsReadOnly();
        
        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParamName { get; private set; } = string.Empty;  // 例如 "Ballast"
        
        /// <summary>
        /// 参数定义
        /// </summary>
        public ParamSchema Schema { get; private set; } 
        
        /// <summary>
        /// 生效日期
        /// </summary>
        public DateTime EffectiveDate { get; private set; }

        private ParamStructure() { }

        /// <summary>
        /// 工厂：创建单参数结构，保证 Schema 至少包含一项主参数定义
        /// </summary>
        public static ParamStructure Create(
            ParamStructureId id,
            IEnumerable<StandardFamilyId?> standardFamilyIds,
            IEnumerable<FormulaId?> formulaIds,
            string paramName,
            ParamSchema schema,
            IEnumerable<ParamRuleId?> ruleIds,
            DateTime? effectiveDate = null)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(paramName))
                throw new ArgumentException("paramName required", nameof(paramName));
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (schema.RequiredParam == null)
                throw new ArgumentException("Schema must contain at least one ParamDefinition", nameof(schema));

            var ps = new ParamStructure
            {
                Id = id,
                ParamName = paramName.Trim(),
                Schema = schema,
                EffectiveDate = effectiveDate ?? DateTime.UtcNow
            };

            // 2. 初始化集合：将传入的 Id 集合添加到私有字段中
            if (standardFamilyIds != null)
            {
                foreach (var familyId in standardFamilyIds.Where(f => f != null))
                {
                    ps._standardFamilyIds.Add(familyId);
                }
            }

            if (formulaIds != null)
            {
                foreach (var formulaId in formulaIds.Where(f => f != null))
                {
                    ps._formulaIds.Add(formulaId);
                }
            }

            if (ruleIds != null) 
            {
                foreach (var ruleId in ruleIds.Where(f => f != null)) 
                {
                    ps._ruleIds.Add(ruleId);
                }
            }

            return ps;
        }

        /// <summary>
        /// 从数据库重建
        /// </summary>
        /// <param name="id"></param>
        /// <param name="familyId"></param>
        /// <param name="formulaId"></param>
        /// <param name="paramName"></param>
        /// <param name="schema"></param>
        /// <param name="ruleIds"></param>
        /// <param name="effectiveDate"></param>
        /// <returns></returns>
        public static ParamStructure Reconstitute(
            ParamStructureId id,
            IEnumerable<StandardFamilyId?> standardFamilyIds, // 3. 修改为集合
            IEnumerable<FormulaId?> formulaIds,               // 4. 修改为集合
            string paramName,
            ParamSchema schema,
            IEnumerable<ParamRuleId>? ruleIds,
            DateTime effectiveDate
            )
        {
            var ps = new ParamStructure
            {
                Id = id,
                ParamName = paramName.Trim(),
                Schema = schema,
                EffectiveDate = effectiveDate
            };

            // 5. 重建集合：将数据库读取的 Id 集合还原到私有字段中
            if (standardFamilyIds != null)
            {
                foreach (var familyId in standardFamilyIds.Where(f => f != null))
                {
                    ps._standardFamilyIds.Add(familyId);
                }
            }

            if (formulaIds != null)
            {
                foreach (var formulaId in formulaIds.Where(f => f != null))
                {
                    ps._formulaIds.Add(formulaId);
                }
            }

            if (ruleIds != null)
            {
                foreach (var ruleId in ruleIds.Where(f => f != null))
                {
                    ps._ruleIds.Add(ruleId);
                }
            }

            return ps;
        }

        /// <summary>
        /// 主参数定义（Schema.RequiredParam）
        /// </summary>
        public ParamDefinition MainParamDefinition => Schema.RequiredParam;

        /// <summary>
        /// 验证一级条件池是否满足结构要求（结构层面）
        /// - 只做字段存在性、白名单基础校验
        /// - 更复杂的语义校验（表达式、数据类型细化）由 Formula/语义分析器完成
        /// </summary>
        public Result ValidateConditionPool(ConditionPool pool)
        {
            if (pool == null) return Result.Fail("ConditionPool is null");

            foreach (var requirement in Schema.ConditionRequirements)
            {
                //存在性验证
                if (requirement.IsRequired && !pool.HasCondition(requirement.FieldName))
                    return Result.Fail($"Missing required condition: {requirement.FieldName}");

                // 白名单验证
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
    }
}
