using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj
{
    /// <summary>
    /// ConditionPoolId使用Guid
    /// </summary>
    public class ConditionPoolId : AggregateRootId
    {
        public Guid Value { get; private set; }

        public ConditionPoolId(Guid value)
        {
            if (value == Guid.Empty) throw new ArgumentException("ConditionPoolId cannot be empty", nameof(value));
            Value = value;
        }

        public override string ToString() => Value.ToString();
    }
}
