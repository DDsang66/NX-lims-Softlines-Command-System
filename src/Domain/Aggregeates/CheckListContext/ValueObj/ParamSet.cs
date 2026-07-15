using System;
using System.Collections.Generic;
namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj
{
    public class ParamSet
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, object?> Values => _values;

        public void SetValueOrFallback(string name, object value, object fallbackValue)
        {
            _values[name] = value ?? fallbackValue;
        }

        public bool TryGetValue(string name, out object? value) => _values.TryGetValue(name, out value);

        public bool Contains(string name) => _values.ContainsKey(name);
    }
}
