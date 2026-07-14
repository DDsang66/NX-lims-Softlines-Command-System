using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj
{
    public class FormulaId:AggregateRootId
    {
        public string Value { get; private set; }

        public FormulaId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("FormulaId required", nameof(value));
            Value = value;
        }
        public override string ToString() => Value.ToString();
    }
}
