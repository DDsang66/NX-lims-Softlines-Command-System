using NX_lims_Softlines_Command_System.Domain.Shared.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext
{
    public sealed class Formula : IAggregateRoot
    {
        public FormulaId Id { get; private set; }
        public StandardFamilyId FamilyId { get; private set; }  // 所属标准族
        public string Name { get; private set; }  // "BallastDerivation"
        public string ParamName { get; private set; }  // 生成的参数名 "Ballast"
        public List<string> ConditionFields { get; private set; }  // ["FiberDominantType", "BuyerSpecified"]
        public string ExpressionTemplate { get; private set; }  // "FiberDominantType + BuyerSpecified"
        public string Description { get; private set; }
        public int Version { get; private set; }  // 版本号
        public DateTime EffectiveDate { get; private set; }  // 生效日期
        public bool IsActive { get; private set; }

        private Formula() { }

        /// <summary>
        /// 创建 Formula 聚合根的实例（工厂方法，仅在内存中创建并保证不变式）
        /// 持久化请通过 IFormulaRepository 在应用层完成（例如 repository.Add(formula) 后提交事务）
        /// </summary>
        public static Formula Create(
            FormulaId id, 
            string name, 
            string paramName, 
            IEnumerable<string> conditionFields,
            string expressionTemplate, string? description = null)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
            if (string.IsNullOrWhiteSpace(paramName)) throw new ArgumentException("ParamName required", nameof(paramName));
            if (conditionFields == null) throw new ArgumentNullException(nameof(conditionFields));

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
                IsActive = true,
                Version = 1,
                EffectiveDate = DateTime.UtcNow
            };

            // 如果需要发布领域事件，可在应用层或这里添加：
            // f.AddDomainEvent(new FormulaCreatedEvent(f.Id.Value));

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
        /// 激活公式，使其参与计算。通常在创建或修改公式后需要调用此方法来启用公式的计算功能。
        /// </summary>
        public void Activate() => IsActive = true;

        /// <summary>
        /// 管理公式的条件集合（保持不变式）
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        public Result ValidateConditionPool(ConditionPool pool)
        {
            //对条件池验证置与该聚合根的原因是，当前位置直接检测已富化条件池的完整性
            //避免在计算公式时出现缺失条件的情况。
            //Formula 描述“哪些原子条件构成该参数的推导范式”，把需要的字段、类型和基本语义放在 Formula 可避免多处重复定义。
            var missing = ConditionFields
                .Where(f => !pool.HasCondition(f))
                .ToList();

            return missing.Any()
                ? Result.Fail("missing")
                : Result.Ok();
        }

        //•	发布领域事件：FormulaCreated、FormulaUpdated、FormulaActivated 等，通知规则/结构需要重新编译或同步
    }
}
