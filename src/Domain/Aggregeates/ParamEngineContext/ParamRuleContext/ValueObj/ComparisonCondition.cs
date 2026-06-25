using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj
{
    public class ComparisonCondition
    {
        public string FieldPath { get; set; } = string.Empty; // 支持路径如 "FiberContent.Polyester"
        public ComparisonOperator Operator { get; set; } //运算符
        public object? ExpectedValue { get; set; } //期望值

        public ComparisonCondition() { }

        public ComparisonCondition(string fieldPath, ComparisonOperator op, object? expectedValue)
        {
            FieldPath = fieldPath ?? throw new ArgumentNullException(nameof(fieldPath));
            Operator = op;
            ExpectedValue = expectedValue;
        }
    }
}
