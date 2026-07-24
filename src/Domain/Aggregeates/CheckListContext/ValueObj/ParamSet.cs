using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using System;
using System.Collections.Generic;
namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj
{
    public class ParamSet:ValueObject
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, object?> Values => _values;

        public void SetValueOrFallback(string name, object value, object fallbackValue)
        {
            _values[name] = value ?? fallbackValue;
        }

        /// <summary>
        /// 支持从数据库重建
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static ParamSet Reconstruct(Dictionary<string, object?> values)
        {
            var paramSet = new ParamSet();
            foreach (var kvp in values)
            {
                paramSet._values[kvp.Key] = kvp.Value;
            }
            return paramSet;
        }

        public bool TryGetValue(string name, out object? value) => _values.TryGetValue(name, out value);

        public bool Contains(string name) => _values.ContainsKey(name);

        /// <summary>
        /// 合并另一个 ParamSet 的值
        /// - 遇到同名键：默认覆盖（后入优先）
        /// - 可选冲突策略
        /// </summary>
        public void Merge(ParamSet other, MergeConflictStrategy strategy = MergeConflictStrategy.Overwrite)
        {
            if (other == null) return;

            foreach (var (key, value) in other.Values)
            {
                if (_values.ContainsKey(key))
                {
                    switch (strategy)
                    {
                        case MergeConflictStrategy.Overwrite:
                            _values[key] = value;
                            break;
                        case MergeConflictStrategy.Ignore:
                            // 保留当前值，跳过
                            continue;
                        case MergeConflictStrategy.Throw:
                            throw new InvalidOperationException($"Merge conflict: key '{key}' already exists in ParamSet");
                        case MergeConflictStrategy.CombineList:
                            _values[key] = CombineValues(_values[key], value);
                            break;
                    }
                }
                else
                {
                    _values[key] = value;
                }
            }
        }

        /// <summary>
        /// 合并另一个 ParamSet 并返回新实例（不可变方式）
        /// </summary>
        public ParamSet MergeNew(ParamSet other, MergeConflictStrategy strategy = MergeConflictStrategy.Overwrite)
        {
            var merged = new ParamSet();

            // 先复制当前值
            foreach (var (key, value) in _values)
            {
                merged._values[key] = value;
            }

            // 再合并其他
            merged.Merge(other, strategy);

            return merged;
        }

        /// <summary>
        /// 尝试合并，返回是否成功（无冲突时）
        /// </summary>
        public bool TryMerge(ParamSet other, out string? conflictKey)
        {
            conflictKey = null;

            foreach (var key in other.Values.Keys)
            {
                if (_values.ContainsKey(key))
                {
                    conflictKey = key;
                    return false;
                }
            }

            Merge(other, MergeConflictStrategy.Overwrite);
            return true;
        }

        // ========== 私有辅助 ==========

        private static object? CombineValues(object? existing, object? incoming)
        {
            // 如果都是列表，合并列表
            if (existing is System.Collections.IEnumerable existingList &&
                incoming is System.Collections.IEnumerable incomingList &&
                existing is not string &&
                incoming is not string)
            {
                var combined = new System.Collections.ArrayList();
                foreach (var item in existingList) combined.Add(item);
                foreach (var item in incomingList) combined.Add(item);
                return combined;
            }

            // 默认：转为列表包装
            var list = new System.Collections.ArrayList();
            if (existing != null) list.Add(existing);
            if (incoming != null) list.Add(incoming);
            return list.Count > 0 ? list : null;
        }


        protected override IEnumerable<object> GetEqualityComponents() 
        {
            yield return Values;
        }
    }
}
