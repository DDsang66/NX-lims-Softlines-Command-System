using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj
{
    /// <summary>
    /// ConditionPoolId使用Guid
    /// </summary>
    public class ConditionPoolId : AggregateRootId<Guid>
    {
        public ConditionPoolId(Guid value)
            :base(value) 
        {
            if (value == Guid.Empty) 
                throw new ArgumentNullException("ConditionPoolId cannot be empty", nameof(value));

        }
    }
}
