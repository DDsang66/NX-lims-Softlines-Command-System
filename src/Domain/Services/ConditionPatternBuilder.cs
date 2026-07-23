using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    public class ConditionPatternBuilder : IConditionPatternBuilder,IScopedDependency
    {
        private readonly ConditionPattern _pattern = new ConditionPattern();

        /// <summary>
        /// 添加等于条件规则
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public IConditionPatternBuilder AddEqual(string field, object? value)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException(nameof(field));

            _pattern.AddEqual(field, value);
            return this;
        }

        /// <summary>
        /// 添加比较条件规则
        /// </summary>
        /// <param name="fieldPath"></param>
        /// <param name="op"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public IConditionPatternBuilder AddComparison(string fieldPath, ComparisonOperator op, object? value)
        {
            if (string.IsNullOrWhiteSpace(fieldPath))
                throw new ArgumentException(nameof(fieldPath));

            _pattern.AddComparison(fieldPath, op, value);
            return this;
        }
        
        /// <summary>
        /// 添加包含条件规则
        /// </summary>
        /// <param name="field"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public IConditionPatternBuilder AddIn(string field, IEnumerable<object?> values)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException(nameof(field));

            _pattern.AddIn(field, values);
            return this;
        }

        /// <summary>
        /// 添加复合条件规则
        /// </summary>
        /// <param name="composite"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public IConditionPatternBuilder AddComposite(CompositeCondition composite)
        {
            if (composite == null)
                throw new ArgumentNullException(nameof(composite));

            _pattern.AddComposite(composite);
            return this;
        }

        public ConditionPattern Build()
        {
            // 验证构建结果
            ValidatePattern();
            return _pattern;
        }

        private void ValidatePattern()
        {
            // 确保至少有一种匹配模式
            if (!_pattern.EqualMatches.Any() &&
                !_pattern.ComparisonMatches.Any() &&
                !_pattern.InMatches.Any() &&
                !_pattern.CompositeMatches.Any())
            {
                throw new InvalidOperationException("ConditionPattern must have at least one match type");
            }
        }
    }
}
