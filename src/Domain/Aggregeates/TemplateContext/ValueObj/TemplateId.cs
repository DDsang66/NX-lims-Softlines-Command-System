using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj
{
    public class TemplateId : AggregateRootId<string>
    {
        public TemplateId(string value)
            : base(value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("TemplateId (value) cannot be empty", nameof(value));
        }
    }
}
