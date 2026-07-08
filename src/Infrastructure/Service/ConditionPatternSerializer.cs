using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{
    /// <summary>
    /// ConditionPattern JSON 序列化实现
    /// </summary>
    public sealed class ConditionPatternSerializer : IConditionPatternSerializer, IScopedDependency
    {
        public JsonObject SerializeEqual(string field, object? value)
        {
            return new JsonObject
            {
                ["field"] = field,
                ["value"] = JsonValue.Create(value)
            };
        }

        public JsonObject SerializeComparison(string fieldPath, ComparisonOperator op, object? value)
        {
            return new JsonObject
            {
                ["fieldPath"] = fieldPath,
                ["operator"] = op.ToString(),
                ["expectedValue"] = JsonValue.Create(value)
            };
        }

        public JsonObject SerializeIn(string field, IEnumerable<object?> values)
        {
            return new JsonObject
            {
                ["field"] = field,
                ["values"] = JsonSerializer.SerializeToNode(values) as JsonArray ?? new JsonArray()
            };
        }

        public JsonObject SerializeComposite(CompositeCondition composite)
        {
            return JsonSerializer.SerializeToNode(composite) as JsonObject ?? new JsonObject();
        }

        public JsonObject BuildPattern(
            IEnumerable<(string field, object? value)>? equals = null,
            IEnumerable<(string fieldPath, ComparisonOperator op, object? value)>? comparisons = null,
            IEnumerable<(string field, IEnumerable<object?> values)>? ins = null,
            IEnumerable<CompositeCondition>? composites = null)
        {
            var pattern = new JsonObject();

            // EqualMatches
            if (equals?.Any() == true)
            {
                var equalObj = new JsonObject();
                foreach (var (field, value) in equals)
                    equalObj[field] = JsonValue.Create(value);
                pattern["EqualMatches"] = equalObj;
            }

            // ComparisonMatches
            if (comparisons?.Any() == true)
            {
                var compArray = new JsonArray();
                foreach (var (fieldPath, op, value) in comparisons)
                    compArray.Add(SerializeComparison(fieldPath, op, value));
                pattern["ComparisonMatches"] = compArray;
            }

            // InMatches
            if (ins?.Any() == true)
            {
                var inObj = new JsonObject();
                foreach (var (field, values) in ins)
                    inObj[field] = JsonSerializer.SerializeToNode(values.ToList());
                pattern["InMatches"] = inObj;
            }

            // CompositeMatches
            if (composites?.Any() == true)
            {
                var compArray = new JsonArray();
                foreach (var composite in composites)
                    compArray.Add(SerializeComposite(composite));
                pattern["CompositeMatches"] = compArray;
            }

            return pattern;
        }
    }
}
