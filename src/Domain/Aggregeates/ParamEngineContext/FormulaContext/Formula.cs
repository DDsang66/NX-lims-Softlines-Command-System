using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext
{
    public sealed class Formula : AggregateRoot<FormulaId,string>
    {
        private readonly List<ParamStructureId?> _paramSturctureIds = new();

        private readonly List<StandardFamilyId?> _standardFamilyIds = new();
        /// <summary>
        /// 公式ID
        /// </summary>
        //public FormulaId Id { get; private set; }

        /// <summary>
        /// 参数结构 Id
        /// </summary>
        public IReadOnlyCollection<ParamStructureId?> ParamSturctureIds => _paramSturctureIds.AsReadOnly();

        /// <summary>
        /// 标准族 Id
        /// </summary>
        public IReadOnlyCollection<StandardFamilyId?> StandardFamilyIds => _standardFamilyIds.AsReadOnly();

        /// <summary>
        /// 公式名称
        /// </summary>
        public string Name { get; private set; }  // "BallastDerivation"

        /// <summary>
        /// 生成参数名
        /// </summary>
        public string ParamName { get; private set; }  // 生成的参数名 "Ballast"

        /// <summary>
        /// 条件字段
        /// </summary>
        public List<string> ConditionFields { get; private set; }  // ["FiberDominantType", "BuyerSpecified"]等具体语义的字段名(不可再切割)
      
        /// <summary>
        /// 公式模板
        /// </summary>
        public string ExpressionTemplate { get; private set; }  // "FiberDominantType + BuyerSpecified ->Ballst" 范式样本
        
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; private set; }  // 版本号

        /// <summary>
        /// 生效日期
        /// </summary>
        public DateTime EffectiveDate { get; private set; }  // 生效日期

        /// <summary>
        /// 公式是否启用
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 创建 Formula 聚合根的实例（工厂方法，仅在内存中创建并保证不变式）
        /// 持久化请通过 IFormulaRepository 在应用层完成（例如 repository.Add(formula) 后提交事务）
        /// </summary>
        public static Formula Create(
            FormulaId id,
            string name,
            string paramName,
            IEnumerable<StandardFamilyId?> standardFamilyIds,
            IEnumerable<ParamStructureId?> paramStructureIds,
            IEnumerable<string> conditionFields,
            string expressionTemplate,
            string? description = null
            )
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name required", nameof(name));
            if (string.IsNullOrWhiteSpace(paramName))
                throw new ArgumentException("ParamName required", nameof(paramName));
            if (conditionFields == null)
                throw new ArgumentNullException(nameof(conditionFields));

            // 规范化、去重并校验字段名
            var fields = conditionFields
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (fields.Count == 0)
                throw new ArgumentException("At least one condition field is required", nameof(conditionFields));

            var f = new Formula
            {
                Id = id,
                Name = name.Trim(),
                ParamName = paramName.Trim(),
                ConditionFields = fields,
                ExpressionTemplate = expressionTemplate ?? string.Empty,
                Description = description?.Trim(),
                IsActive = false,
                Version = 1,
                EffectiveDate = DateTime.UtcNow,
            };

            // 3. 初始化 StandardFamilyIds 集合
            if (standardFamilyIds != null)
            {
                foreach (var familyId in standardFamilyIds.Where(fid => fid != null))
                {
                    f._standardFamilyIds.Add(familyId);
                }
            }

            if (paramStructureIds != null) 
            {
                foreach (var paramStructureId in paramStructureIds.Where(psid => psid != null))
                {
                    f._paramSturctureIds.Add(paramStructureId);
                }
            }

            return f;
        }


        /// <summary>
        /// 根据持久化数据重新构建 Formula 聚合根的实例（工厂方法，仅在内存中创建并保证不变式）
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="paramName"></param>
        /// <param name="conditionFields"></param>
        /// <param name="familyId"></param>
        /// <param name="expressionTemplate"></param>
        /// <param name="description"></param>
        /// <param name="version"></param>
        /// <param name="isActive"></param>
        /// <param name="effectiveDate"></param>
        /// <returns></returns>
        internal static Formula Reconstitute(
           FormulaId id,
           string name,
           string paramName,
           IEnumerable<string> conditionFields,
           IEnumerable<StandardFamilyId?> standardFamilyIds,
           IEnumerable<ParamStructureId?> paramStructureIds,
           string expressionTemplate,
           int version,
           bool isActive,
           DateTime effectiveDate,
           string? description = null)
        {
            var f = new Formula
            {
                Id = id,
                Name = name,
                ParamName = paramName,
                ConditionFields = conditionFields.ToList(),
                ExpressionTemplate = expressionTemplate,
                Description = description,
                Version = version,
                IsActive = isActive,
                EffectiveDate = effectiveDate
            };

            // 5. 重建 StandardFamilyIds 集合
            if (standardFamilyIds != null)
            {
                foreach (var familyId in standardFamilyIds.Where(fid => fid != null))
                {
                    f._standardFamilyIds.Add(familyId);
                }
            }

            if (paramStructureIds != null)
            {
                foreach (var paramStructureId in paramStructureIds.Where(psid => psid != null))
                {
                    f._paramSturctureIds.Add(paramStructureId);
                }
            }

            return f;
        }



        /// <summary>
        /// 返回公式声明的原子条件字段名（供前置验证）
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> RequiredConditions() => ConditionFields.AsReadOnly();

        /// <summary>
        /// 关闭公式，使其不参与计算。通常在不需要使用某个公式时，可以调用此方法来禁用公式的计算功能。
        /// </summary>
        public void Deactivate() => IsActive = false;

        /// <summary>
        /// 激活公式，使其参与计算。
        /// 通常在创建或修改公式后需要调用此方法来启用公式的计算功能。
        /// </summary>
        public void Activate()
        {
            // 1. 必须有归属
            if (_standardFamilyIds.Count == 0)
                throw new InvalidOperationException("Formula must be attached to at least one StandardFamily before activation");

            // 2. 必须有表达式模板
            if (string.IsNullOrWhiteSpace(ExpressionTemplate))
                throw new InvalidOperationException("ExpressionTemplate is required for activation");

            // 3. 必须有条件字段
            if (ConditionFields == null || ConditionFields.Count == 0)
                throw new InvalidOperationException("At least one condition field is required for activation");

            // 4. 校验表达式模板语法
            //if (!ValidateExpressionTemplate())
            //    throw new InvalidOperationException("Invalid expression template format");

            IsActive = true;
        }


        /// <summary>
        /// 添加关联的标准族
        /// </summary>
        public void AddStandardFamily(StandardFamilyId familyId)
        {
            if (familyId == null) throw new ArgumentNullException(nameof(familyId));
            if (!_standardFamilyIds.Contains(familyId)) // 保证幂等性/去重
            {
                _standardFamilyIds.Add(familyId);
            }
        }

        /// <summary>
        /// 移除关联的标准族
        /// </summary>
        public void RemoveStandardFamily(StandardFamilyId familyId)
        {
            if (familyId == null) throw new ArgumentNullException(nameof(familyId));
            _standardFamilyIds.Remove(familyId);
        }

        /// <summary>
        /// 添加关联的参数结构
        /// </summary>
        public void AddParamStructure(ParamStructureId paramStructureId)
        {
            if (paramStructureId == null) throw new ArgumentNullException(nameof(paramStructureId));
            if (!_paramSturctureIds.Contains(paramStructureId))
            {
                _paramSturctureIds.Add(paramStructureId);
            }
        }

        /// <summary>
        /// 移除关联的参数结构
        /// </summary>
        public void RemoveParamStructure(ParamStructureId paramStructureId)
        {
            if (paramStructureId == null) throw new ArgumentNullException(nameof(paramStructureId));
            _paramSturctureIds.Remove(paramStructureId);
        }

    }
}
