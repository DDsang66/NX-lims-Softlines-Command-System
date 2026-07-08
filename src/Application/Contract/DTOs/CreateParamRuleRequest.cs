namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    public record CreateParamRuleRequest
    {
        public string Id { get; set; }
        public string FormulaId { get; set; }
        public string ParamName { get; set; }
        public string ParamResult { get; set; }
        public bool StopOnMatch { get; set; }
        public int Priority { get; set; }
        public List<EqualMatchDto> EqualMatches { get; set; } = new();
        public List<ComparisonMatchDto> ComparisonMatches { get; set; } = new();
        public List<InMatchDto> InMatches { get; set; } = new();
        public List<CompositeConditionDto> CompositeMatches { get; set; } = new();
    }

    public class EqualMatchDto
    {
        public string Field { get; set; }
        public object Value { get; set; }
    }

    public class ComparisonMatchDto
    {
        public string FieldPath { get; set; }
        public string Operator { get; set; }
        public object ExpectedValue { get; set; }
    }

    public class InMatchDto
    {
        public string Field { get; set; }
        public List<object> Values { get; set; } = new();
    }

    public class CompositeConditionDto
    {
        public string Logic { get; set; }
        public List<string> FieldNames { get; set; } = new();
        public List<ComparisonMatchDto> SubConditions { get; set; } = new();
    }
}
