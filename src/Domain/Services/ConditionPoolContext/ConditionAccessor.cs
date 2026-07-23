using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Domain.Services.ConditionPoolContext
{
    /// <summary>
    /// 条件访问器
    /// </summary>
    public class ConditionAccessor : IConditionAccessor, IScopedDependency
    {
        public bool TryGet(ConditionPool pool, string path, out object? value)
        {
            value = null;
            if (pool == null || string.IsNullOrWhiteSpace(path)) return false;

            // 直接键
            if (pool.Conditions.TryGetValue(path, out var direct))
            {
                value = direct;
                return true;
            }

            // 嵌套路径 A.B.C
            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            if (!pool.Conditions.TryGetValue(parts[0], out var current)) return false;
            if (parts.Length == 1) { value = current; return true; }

            var dict = current as IDictionary<string, object?>;
            for (int i = 1; i < parts.Length; i++)
            {
                if (dict == null) return false;
                var key = parts[i];
                if (!dict.TryGetValue(key, out current)) return false;
                if (i == parts.Length - 1) { value = current; return true; }
                dict = current as IDictionary<string, object?>;
            }

            return false;
        }
    }
}
