using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext
{
    public class TestConditionTextTemplate:Entity
    {
        /// <summary>
        /// 模板索引
        /// </summary>
        public TemplateIndex TemplateIndex { get; private set; }

        /// <summary>
        /// 文本模板
        /// 例如： "Procedure No: {WashProcedure}; Using horizontal axis, front-loading type machie: Machine wash at {WashingTemperature} degree C with {WashLoad} kg total dry mass( {BallastType} + specimen) and {ReferenceDetergentsComposition}, {DryProcedure}, / Iron.";
        /// 目的是为了提供样本插入参数中的值，生成完整的测试条件
        /// </summary>
        public string Text { get; private set; } = string.Empty;

        // EF Core 需要
        private TestConditionTextTemplate() { }

        private TestConditionTextTemplate(
            TemplateIndex templateIndex,
            string text)
        {
            TemplateIndex = templateIndex;
            Text = text;
        }

        /// <summary>
        /// 创建测试条件文本模板
        /// </summary>
        public static TestConditionTextTemplate Create(
            Dictionary<string, object> templateIndex,
            string text)
        {
            if (templateIndex == null)
                throw new ArgumentNullException(nameof(templateIndex), "模板索引不能为空");

            if (templateIndex.Values.Count == 0)
                throw new ArgumentException("模板索引不能为空索引", nameof(templateIndex));

            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("文本模板不能为空", nameof(text));

            var index = TemplateIndex.Create(templateIndex); // 假设 TemplateIndex 提供了 Create 工厂方法
            
            return new TestConditionTextTemplate(index, text.Trim());
        }

        /// <summary>
        /// 创建测试条件文本模板
        /// </summary>
        public static TestConditionTextTemplate Rebuild(
            TemplateIndex templateIndex,
            string text)
        {
            if (templateIndex == null)
                throw new ArgumentNullException(nameof(templateIndex), "模板索引不能为空");

            if (templateIndex.Values.Count == 0)
                throw new ArgumentException("模板索引不能为空索引", nameof(templateIndex));

            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("文本模板不能为空", nameof(text));

            return new TestConditionTextTemplate(templateIndex, text.Trim());
        }

        /// <summary>
        /// 更新文本模板内容
        /// </summary>
        public void UpdateText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("文本模板不能为空", nameof(text));

            Text = text.Trim();
        }

        /// <summary>
        /// 更新索引（谨慎使用，可能影响已引用它的 Template 查找）
        /// </summary>
        public void UpdateIndex(TemplateIndex templateIndex)
        {
            if (templateIndex == null)
                throw new ArgumentNullException(nameof(templateIndex));

            if (templateIndex.Values.Count == 0)
                throw new ArgumentException("模板索引不能为空索引", nameof(templateIndex));

            TemplateIndex = templateIndex;
        }

        /// <summary>
        /// 判断该文本模板是否匹配给定查询条件
        /// </summary>
        public bool Matches(IReadOnlyDictionary<string, object?> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                return false;

            foreach (var kvp in conditions)
            {
                // 模板索引里没有这个 key → 跳过，不当作不匹配
                if (!TemplateIndex.Values.TryGetValue(kvp.Key, out var indexValue))
                    continue;

                if (!AreEqual(indexValue, kvp.Value))
                    return false;
            }

            return true;
        }

        private static bool AreEqual(object? a, object? b)
        {
            a = NormalizeJsonElement(a);
            b = NormalizeJsonElement(b);

            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            if (a is string sa && b is string sb)
                return string.Equals(sa.Trim(), sb.Trim(), StringComparison.OrdinalIgnoreCase);

            if (IsNumeric(a) && IsNumeric(b))
                return Convert.ToDecimal(a) == Convert.ToDecimal(b);

            return a.Equals(b);
        }

        private static object? NormalizeJsonElement(object? v)
        {
            if (v is System.Text.Json.JsonElement je)
            {
                return je.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => je.GetString(),
                    System.Text.Json.JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDecimal(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => null,
                    _ => je.GetRawText()
                };
            }
            return v;
        }

        private static bool IsNumeric(object value)
        {
            return value is byte or sbyte or short or ushort or int or uint
                or long or ulong or float or double or decimal;
        }
    }
}
