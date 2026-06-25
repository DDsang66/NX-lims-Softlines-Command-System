using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Conparison
{
    /// <summary>
    /// 值比较器接口
    /// </summary>
    public interface IValueComparer
    {
        bool AreEqual(object? a, object? b);
        bool Compare(object? a, ComparisonOperator op, object? b);
        bool TryConvertToDecimal(object? v, out decimal d);
        bool IsTruthy(object? v);
    }
}
