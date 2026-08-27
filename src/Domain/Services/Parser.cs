using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;
using System.Globalization;

using System.Text.RegularExpressions;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    /// <summary>
    /// 领域服务：规则解析器
    /// 协调 Token 序列与 Formula 范式的匹配
    /// 负责业务规则校验和语义推导
    /// </summary>
    public class Parser : IParser, IScopedDependency
    {

        private readonly IConditionPatternSerializer _serializer;

        public Parser(IConditionPatternSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// ============================================================
        /// 主入口方法：解析规则文本
        /// ============================================================
        /// 兼容两种模板：槽位语法（SlotType{f1,f2}+... -> R）与旧式逐字段语法（f1 + f2 -> R）
        /// 流程说明：
        /// 步骤1：校验推导符（→）是否存在
        /// 步骤2：分割左右两侧（左侧=条件，右侧=结果）
        /// 步骤3：按 '+' 分割左侧为多个槽位
        /// 步骤4：判断使用槽位语法还是旧式语法
        /// 步骤5：根据语法类型构建条件列表
        /// 步骤6：序列化为 Pattern JSON
        /// 步骤7：构建并返回解析结果
        /// ============================================================
        /// </summary>
        public ParsedRule Parse(string rawText, IReadOnlyList<Token> tokens, Formula formula)
        {
            // ==================== 步骤1：校验推导符 ====================
            // 在 Token 列表中查找推导符（→）的索引位置
            // 推导符用于分隔"条件"和"结果"
            var arrowIndex = FindRangeOperator(tokens);

            // ==================== 步骤2：分割左右 ====================
            // 左侧 Tokens：推导符之前的部分，代表条件表达式
            var leftTokens = tokens.Take(arrowIndex).ToList();

            // 右侧 Tokens：推导符之后的部分，代表结果值
            var rightTokens = tokens.Skip(arrowIndex + 1).ToList();

            // ==================== 步骤3：按 '+' 分割槽位 ====================
            // 将左侧条件按 '+' 拆分为多个独立的槽位（Slot）
            // 注意：会忽略大括号内部的 '+'，支持嵌套结构如 Slot{a,b,c}
            var slotValues = SplitByDelimiter(leftTokens);

            // ==================== 步骤4：判断模板类型 ====================
            // 检查 Formula 的 ExpressionTemplate 是否包含大括号 {}
            // 包含大括号 => 槽位语法（如 "Equal{f1}+Comparer{f2}->R"）
            // 不包含大括号 => 旧式逐字段语法（如 "f1+f2->R"）
            var useSlotSyntax = !string.IsNullOrWhiteSpace(formula.ExpressionTemplate)
                                && formula.ExpressionTemplate.Contains("{") && formula.ExpressionTemplate.Contains("}");

            // 初始化各类条件集合
            var equals = new List<(string field, object? value)>();
            var comparisons = new List<(string fieldPath, ComparisonOperator op, object? value)>();
            var ins = new List<(string field, IEnumerable<object?> values)>();
            var composites = new List<CompositeCondition>();

            // ==================== 步骤5：根据语法类型构建条件 ====================
            if (useSlotSyntax)
            {
                // ---------- 5.1 槽位语法模式 ----------
                // 从模板解析出槽顺序与每槽的字段名
                // 例如 "Equal{Field1,Field2}+Comparer{Field3}->R" 
                // 解析为 [(Equal, ["Field1","Field2"]), (Comparer, ["Field3"])]
                var parsedSlots = ParseTemplateSlots(formula.ExpressionTemplate);

                // 校验槽位数量是否匹配
                if (slotValues.Count != parsedSlots.Count)
                    throw new Exception($"范式要求 {parsedSlots.Count} 个槽位，实际有 {slotValues.Count} 个");

                // 按槽解析每个槽内的值并按字段名填充 Equal 条目
                BuildConditionsFromSlots(slotValues, parsedSlots, equals, comparisons, ins, composites);
            }
            else
            {
                // ---------- 5.2 旧式语法模式 ----------
                // 兼容模式：位置一一对应
                ValidateSlotCount(slotValues, formula.ConditionFields);
                equals = BuildEqualConditions(slotValues, formula.ConditionFields);
            }

            // ==================== 步骤6：组装 Pattern JSON ====================
            // 使用序列化器将条件集合转换为 JSON 格式
            var patternJson = _serializer.BuildPattern(
                equals: equals,
                comparisons: comparisons,
                ins: ins.Any() ? ins : null,
                composites: composites.Any() ? composites : null);

            // ==================== 步骤7：构建结果 ====================
            // 将右侧 Token 拼接为结果值
            string resultValue;

            var firstRightToken = rightTokens.FirstOrDefault();
            var lastRightToken = rightTokens.LastOrDefault();

            if (firstRightToken != null && lastRightToken != null)
            {
                // 【核心修复】：直接从原始文本中截取右侧结果
                // 这样无论词法分析器怎么丢弃空格，都不会影响最终拼接的格式
                int startIndex = firstRightToken.Position;
                int endIndex = lastRightToken.Position + lastRightToken.Value.Length;

                resultValue = rawText.Substring(startIndex, endIndex - startIndex).Trim();
            }
            else
            {
                resultValue = string.Empty; // 右侧无内容
            }

            return new ParsedRule
            {
                ConditionPatternJson = patternJson,
                ResultValue = resultValue,
                SourceText = string.Join("", tokens.Select(t => t.Value))
            };
        }

        /// <summary>
        /// ============================================================
        /// 辅助方法：查找推导符
        /// ============================================================
        /// 流程：
        /// 步骤1：在 Token 列表中查找 TokenType.RangeOperator 类型
        /// 步骤2：如果找不到则抛出异常
        /// 步骤3：返回推导符的索引位置
        /// ============================================================
        /// </summary>
        private int FindRangeOperator(IReadOnlyList<Token> tokens)
        {
            var index = tokens.ToList()
                .FindIndex(t => t.Type == TokenType.RangeOperator);

            if (index < 0) throw new Exception("规则缺少推导符 →");

            return index;
        }

        /// <summary>
        /// ============================================================
        /// 辅助方法：按 '+' 分割槽位（支持嵌套大括号）
        /// ============================================================
        /// 流程：
        /// 步骤1：遍历所有 Token
        /// 步骤2：遇到字符串字面量直接加入当前槽（不参与分割）
        /// 步骤3：遇到 '{' 增加大括号深度，遇到 '}' 减少深度
        /// 步骤4：遇到 '+' 且大括号深度为 0（顶层）时，分割槽位
        /// 步骤5：其他 Token 加入当前槽
        /// 步骤6：返回分割后的槽位列表
        /// ============================================================
        /// </summary>
        private List<List<Token>> SplitByDelimiter(List<Token> tokens)
        {
            var slots = new List<List<Token>>();
            var current = new List<Token>();
            int braceDepth = 0; // 跟踪 { } 嵌套深度

            foreach (var token in tokens)
            {
                // 字符串字面量直接加入当前槽位，不参与分隔
                if (token.Type == TokenType.StringLiteral)
                {
                    current.Add(token);
                    continue;
                }

                // 处理大括号深度（Parenthesis 用于包含 '{' 或 '}'）
                if (token.Type == TokenType.Parenthesis)
                {
                    if (token.Value == "{")
                    {
                        braceDepth++;
                    }
                    else if (token.Value == "}")
                    {
                        if (braceDepth > 0) braceDepth--;
                    }

                    current.Add(token);
                    continue;
                }

                // 仅当 token 为 '+' 且处于顶层（不在大括号内部）时分割槽位
                var isPlus = (token.Type == TokenType.ArithmeticOperator && token.Value == "+");
                if (isPlus && braceDepth == 0)
                {
                    slots.Add(current);
                    current = new List<Token>();
                }
                else
                {
                    current.Add(token);
                }
            }

            if (current.Any()) slots.Add(current);

            return slots;
        }

        /// <summary>
        /// ============================================================
        /// 辅助方法：验证槽位数量
        /// ============================================================
        /// 流程：
        /// 步骤1：检查 slotValues 数量是否等于 conditionFields 数量
        /// 步骤2：不匹配则抛出异常并显示期望的字段列表
        /// ============================================================
        /// </summary>
        private void ValidateSlotCount(List<List<Token>> slotValues, List<string> conditionFields)
        {
            if (slotValues.Count != conditionFields.Count)
                throw new Exception(
                    $"范式要求 {conditionFields.Count} 个条件（{string.Join(", ", conditionFields)}），实际有 {slotValues.Count} 个");
        }


        /// <summary>
        /// ============================================================
        /// 辅助方法：构建等值条件（旧式映射）
        /// ============================================================
        /// 流程：
        /// 步骤1：遍历所有字段
        /// 步骤2：将每个槽位的 Token 拼接为字符串
        /// 步骤3：创建 (字段名, 值) 元组添加到列表
        /// ============================================================
        /// </summary>
        private List<(string field, object? value)> BuildEqualConditions(
            List<List<Token>> slotValues,
            List<string> conditionFields)
        {
            var equals = new List<(string field, object? value)>();

            for (int i = 0; i < conditionFields.Count; i++)
            {
                var fieldName = conditionFields[i];
                var rawValue = string.Join("", slotValues[i].Select(t => t.Value)).Trim();
                equals.Add((fieldName, rawValue));
            }

            return equals;
        }

        /// <summary>
        /// ============================================================
        /// 核心方法：根据槽类型构建条件
        /// ============================================================
        /// 流程：
        /// 步骤1：遍历所有解析出的槽位
        /// 步骤2：提取槽内的实际 Token（去掉大括号和槽名）
        /// 步骤3：按逗号分割为多个值组
        /// 步骤4：根据 SlotType 分类处理：
        ///     - Equal：作为等值条件
        ///     - Comparer：尝试解析比较操作符（>=, <=, >, <, !=, =）
        ///     - Inner：按逗号分割为集合（IN 条件）
        ///     - Composite：预留扩展
        /// ============================================================
        /// </summary>
        private void BuildConditionsFromSlots(
            List<List<Token>> slotValues,
            List<(SlotType slotType, List<string> fields)> parsedSlots,
            List<(string field, object? value)> equals,
            List<(string fieldPath, ComparisonOperator op, object? value)> comparisons,
            List<(string field, IEnumerable<object?> values)> ins,
            List<CompositeCondition> composites)
        {
            for (int i = 0; i < parsedSlots.Count; i++)
            {
                var (slotType, fieldNames) = parsedSlots[i];

                // 提取槽内实际 tokens（去掉可能的槽名/大括号）
                var innerTokens = ExtractSlotInnerTokens(slotValues[i]);

                // 提取槽内实际 tokens（去掉可能的槽名/大括号）
                var tokenGroups = SplitSlotTokensIntoValues(innerTokens);

                // 简单校验：值组数量要与字段数匹配（可根据业务放宽）
                if (tokenGroups.Count != fieldNames.Count)
                    throw new InvalidOperationException($"槽位 '{slotType}' 期望 {fieldNames.Count} 个值，但实际解析到 {tokenGroups.Count} 个。");

                switch (slotType)
                {
                    case SlotType.Equal:
                        // ---------- Equal 槽：等值条件 ----------
                        // 每个字段对应一个值，直接作为等值条件
                        for (int j = 0; j < fieldNames.Count; j++)
                        {
                            var raw = JoinAndNormalizeTokenGroup(tokenGroups[j]);
                            equals.Add((fieldNames[j], TryParseLiteral(raw)));
                        }
                        break;

                    case SlotType.Comparer:
                        // ---------- Comparer 槽：比较条件 ----------
                        for (int j = 0; j < fieldNames.Count; j++)
                        {
                            var raw = JoinAndNormalizeTokenGroup(tokenGroups[j]);

                            // 1) 优先支持用户在规则里直接写 fieldPath+op+value（灵活输入）
                            // 用户直接写 "字段名+操作符+值"，如 "FiberContent.Cellulose>=51"
                            if (TryParseFieldComparison(raw, out var explicitFieldPath, out var explicitOp, out var explicitOperand))
                            {
                                comparisons.Add((explicitFieldPath ?? fieldNames[j], explicitOp, TryParseLiteral(explicitOperand)));
                                continue;
                            }

                            // 2) 否则按模板的 fieldName + 解析到的比较运算处理（例如 ">=51"）
                            // 按模板的 fieldName + 解析到的比较运算，如 ">=51"
                            if (TryParseComparison(raw, out var op, out var operand))
                            {
                                comparisons.Add((fieldNames[j], op, TryParseLiteral(operand)));
                            }
                            else
                            {
                                // 3) 兼容：没有比较符则当等值处理
                                comparisons.Add((fieldNames[j], ComparisonOperator.Equal, TryParseLiteral(raw)));
                            }
                        }
                        break;

                    case SlotType.Inner:
                        // ---------- Inner 槽：IN 条件 ----------
                        // 每个字段对应一个逗号分隔的值列表
                        for (int j = 0; j < fieldNames.Count; j++)
                        {
                            var raw = JoinAndNormalizeTokenGroup(tokenGroups[j]);

                            // 检查是否是数组格式（以 [ 开头，以 ] 结尾）
                            if (raw.TrimStart().StartsWith("[") && raw.TrimEnd().EndsWith("]"))
                            {
                                // 提取方括号内的内容
                                var innerContent = raw.Trim();
                                innerContent = innerContent.Substring(1, innerContent.Length - 2); // 去掉 [ 和 ]

                                // 按逗号分割并解析每个元素
                                var values = innerContent.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                        .Select(v => TryParseLiteral(v.Trim()))
                                                        .ToList<object?>();
                                ins.Add((fieldNames[j], values));
                            }
                            else
                            {
                                // 兼容旧格式：直接按逗号分割
                                var values = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(v => TryParseLiteral(v.Trim()))
                                                .ToList<object?>();
                                ins.Add((fieldNames[j], values));
                            }
                        }
                        break;

                    case SlotType.Composite:
                        // 复合槽的解析可以更复杂，这里暂不实现完整解析，保留占位以便扩展
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported slot type: {slotType}");
                }
            }
        }

        /// <summary>
        /// ============================================================
        /// 辅助方法：将槽内 Token 按逗号分割为多个值组
        /// ============================================================
        /// 流程：
        /// 步骤1：遍历所有 Token
        /// 步骤2：遇到逗号分隔符时分割当前组
        /// 步骤3：否则将 Token 加入当前组
        /// 步骤4：返回分割后的值组列表
        /// ============================================================
        /// 将槽内 tokens 按逗号分割为多个值组（逗号 token 应为 TokenType.Separator 或值为 ","）
        /// 如果没有逗号则整个槽视为单个值组
        /// </summary>
        private List<List<Token>> SplitSlotTokensIntoValues(List<Token> slotTokens)
        {
            var groups = new List<List<Token>>();
            var current = new List<Token>();
            int bracketDepth = 0; // 方括号深度
            int braceDepth = 0;   // 大括号深度（保险）

            foreach (var t in slotTokens)
            {
                // 跟踪方括号深度
                if (t.Type == TokenType.Parenthesis && t.Value == "[")
                {
                    bracketDepth++;
                    current.Add(t);
                    continue;
                }
                else if (t.Type == TokenType.Parenthesis && t.Value == "]")
                {
                    if (bracketDepth > 0) bracketDepth--;
                    current.Add(t);
                    continue;
                }

                // 跟踪大括号深度（处理嵌套对象）
                if (t.Type == TokenType.Parenthesis && t.Value == "{")
                {
                    braceDepth++;
                    current.Add(t);
                    continue;
                }
                else if (t.Type == TokenType.Parenthesis && t.Value == "}")
                {
                    if (braceDepth > 0) braceDepth--;
                    current.Add(t);
                    continue;
                }

                // 只有不在任何括号内部的逗号才是分隔符
                var isComma = (t.Type == TokenType.Separator && t.Value == ",") ||
                              (t.Type == TokenType.ArithmeticOperator && t.Value == ",") ||
                              (t.Type == TokenType.Unknown && t.Value == ",");

                if (isComma && bracketDepth == 0 && braceDepth == 0) // ✅ 关键修复
                {
                    groups.Add(current);
                    current = new List<Token>();
                }
                else
                {
                    current.Add(t);
                }
            }

            if (current.Any()) groups.Add(current);
            return groups;
        }

        /// <summary>
        /// ============================================================
        /// 辅助方法：解析模板中的槽定义
        /// ============================================================
        /// 流程：
        /// 步骤1：验证模板不为空
        /// 步骤2：使用正则提取推导符左侧部分
        /// 步骤3：按 '+' 分割多个槽定义
        /// 步骤4：对每个槽定义，解析 SlotName{field1,field2}
        /// 步骤5：验证 SlotName 是否为有效的枚举值
        /// 步骤6：提取大括号内的字段列表
        /// 步骤7：返回 (SlotType, List<string>) 列表
        /// ============================================================
        /// 示例：
        /// 输入："Equal{Field1,Field2}+Comparer{Field3}->R"
        /// 输出：[(Equal, ["Field1","Field2"]), (Comparer, ["Field3"])]
        /// ============================================================
        /// </summary>
        private List<(SlotType slotType, List<string> fields)> ParseTemplateSlots(string template)
        {
            if (string.IsNullOrWhiteSpace(template))
                throw new ArgumentException("ExpressionTemplate is empty", nameof(template));

            // 找到推导符并取左侧
            var opPattern = @"(.*?)(→|->|=>|~|\bto\b)(.*)";
            var m = System.Text.RegularExpressions.Regex.Match(template, opPattern, System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success)
                throw new InvalidOperationException("ExpressionTemplate must contain a derivation operator like '->','=>','to','~' or '→'.");

            var left = m.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(left))
                throw new InvalidOperationException("ExpressionTemplate left side is empty.");

            var parts = left.Split('+');
            var result = new List<(SlotType slotType, List<string> fields)>();

            foreach (var rawPart in parts)
            {
                var part = rawPart.Trim();
                if (string.IsNullOrWhiteSpace(part))
                    throw new InvalidOperationException("Empty slot definition detected in ExpressionTemplate.");

                var open = part.IndexOf('{');
                var close = part.LastIndexOf('}');
                if (open <= 0 || close <= open)
                    throw new InvalidOperationException($"Invalid slot format: '{part}'. Expected SlotName{{field1,field2}}.");

                var slotName = part.Substring(0, open).Trim();
                if (string.IsNullOrWhiteSpace(slotName))
                    throw new InvalidOperationException($"Invalid slot name '{slotName}' in template.");

                if (!Enum.TryParse<SlotType>(slotName, ignoreCase: true, out var slotType))
                {
                    var allowed = string.Join(", ", Enum.GetNames(typeof(SlotType)));
                    throw new InvalidOperationException($"Invalid slot name '{slotName}' in template. Expected one of: {allowed}.");
                }

                var inner = part.Substring(open + 1, close - open - 1);
                var fields = inner.Split(',')
                                  .Select(f => f.Trim())
                                  .Where(f => !string.IsNullOrWhiteSpace(f))
                                  .ToList();

                if (!fields.Any())
                    throw new InvalidOperationException($"Slot '{slotName}' must contain at least one field.");

                result.Add((slotType, fields));
            }

            return result;
        }

        /// <summary>
        /// ============================================================
        /// 辅助方法：提取槽内的大括号内容
        /// ============================================================
        /// 流程：
        /// 步骤1：查找第一个 '{' 和最后一个 '}' 的位置
        /// 步骤2：如果找到，返回大括号内的 Token
        /// 步骤3：如果没有显式大括号，尝试剥离开头的槽名
        /// 步骤4：否则返回原列表
        /// ============================================================
        /// </summary>
        private List<Token> ExtractSlotInnerTokens(List<Token> slotTokens)
        {
            if (slotTokens == null || slotTokens.Count == 0) return new List<Token>();

            // 查找第一个 '{' 和最后一个 '}'（TokenType.Parenthesis 且 Value 为 '{' / '}'）
            int openIndex = slotTokens.FindIndex(t => t.Type == TokenType.Parenthesis && t.Value == "{");
            int closeIndex = slotTokens.FindLastIndex(t => t.Type == TokenType.Parenthesis && t.Value == "}");

            if (openIndex >= 0 && closeIndex > openIndex)
            {
                // 返回大括号内的 tokens（不含 '{' 与 '}'）
                return slotTokens.Skip(openIndex + 1).Take(closeIndex - openIndex - 1).ToList();
            }

            // 如果没有显式大括号，尝试剥离开头可能的槽名（第一个 Identifier 后跟 Parenthesis）
            if (slotTokens.Count >= 2 && slotTokens[0].Type == TokenType.Identifier
                && slotTokens[1].Type == TokenType.Parenthesis && slotTokens[1].Value == "{")
            {
                openIndex = 1;
                closeIndex = slotTokens.FindLastIndex(t => t.Type == TokenType.Parenthesis && t.Value == "}");
                if (closeIndex > openIndex)
                    return slotTokens.Skip(openIndex + 1).Take(closeIndex - openIndex - 1).ToList();
            }

            // 否则，尝试去掉开头的槽名（若第一个 token 是槽名）并返回剩余
            if (slotTokens[0].Type == TokenType.Identifier && slotTokens.Count > 1)
            {
                return slotTokens.Skip(1).ToList();
            }

            return slotTokens;
        }

        /// <summary>
        /// ============================================================
        /// 辅助方法：将 Token 组拼接为原始文本
        /// ============================================================
        /// 流程：
        /// 步骤1：提取每个 Token 的 Value
        /// 步骤2：拼接为字符串
        /// 步骤3：去除首尾空格
        /// ============================================================
        /// </summary>
        private static string JoinAndNormalizeTokenGroup(List<Token> tokens)
        {
            return string.Join("", tokens.Select(t => t.Value)).Trim();
        }

        /// <summary>
        /// ============================================================
        /// 辅助方法：将文本解析为字面量
        /// ============================================================
        /// 流程：
        /// 步骤1：如果为空则返回 null
        /// 步骤2：如果被引号包裹，去除引号返回字符串
        /// 步骤3：尝试解析为 decimal 数字
        /// 步骤4：如果都失败，保持原始文本
        /// ============================================================
        /// </summary>
        private static object? TryParseLiteral(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            // 删除外层引号（如果有）
            if (raw.StartsWith("\"") && raw.EndsWith("\"") && raw.Length >= 2)
                return raw.Substring(1, raw.Length - 2);

            // 尝试数字
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;

            // 保持原始文本（例如 "77% IEC(A)"）
            return raw;
        }

        /// <summary>
        /// ============================================================
        /// 辅助方法：从文本中解析比较操作符与操作数
        /// ============================================================
        /// 流程：
        /// 步骤1：使用正则匹配比较操作符（>=, <=, ==, !=, >, <, =）
        /// 步骤2：如果匹配成功，解析出操作符和操作数
        /// 步骤3：将操作符字符串映射为 ComparisonOperator 枚举
        /// ============================================================
        /// 示例：">=50" -> (GreaterThanOrEqual, "50")
        /// ============================================================
        /// </summary>
        private static bool TryParseComparison(string raw, out ComparisonOperator op, out string operand)
        {
            op = ComparisonOperator.Equal;
            operand = raw.Trim();

            if (string.IsNullOrWhiteSpace(raw)) return false;

            var pattern = @"^\s*(>=|<=|==|!=|>|<|=)\s*(.+)$";
            var m = Regex.Match(raw, pattern);
            if (!m.Success)
            {
                // 也兼容写成 like '50-60' 等复杂格式（不解析操作符）
                return false;
            }

            var sym = m.Groups[1].Value;
            operand = m.Groups[2].Value.Trim();

            op = sym switch
            {
                ">=" => ComparisonOperator.GreaterThanOrEqual,
                "<=" => ComparisonOperator.LessThanOrEqual,
                "==" or "=" => ComparisonOperator.Equal,
                "!=" => ComparisonOperator.NotEqual,
                ">" => ComparisonOperator.GreaterThan,
                "<" => ComparisonOperator.LessThan,
                _ => ComparisonOperator.Equal
            };

            return true;
        }

        /// <summary>
        /// ============================================================
        /// 辅助方法：从文本中解析比较操作符与操作数
        /// ============================================================
        /// 流程：
        /// 步骤1：使用正则匹配比较操作符（>=, <=, ==, !=, >, <, =）
        /// 步骤2：如果匹配成功，解析出操作符和操作数
        /// 步骤3：将操作符字符串映射为 ComparisonOperator 枚举
        /// ============================================================
        /// 示例：">=50" -> (GreaterThanOrEqual, "50")
        /// ============================================================
        /// </summary>
        private static bool TryParseFieldComparison(string raw, out string? fieldPath, out ComparisonOperator op, out string operand)
        {
            fieldPath = null;
            op = ComparisonOperator.Equal;
            operand = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            // 允许字段路径包含点号（A.B），例如 "FiberContent.Cellulose>=51"
            var pattern = @"^\s*([\w\.]+)\s*(>=|<=|==|!=|>|<|=)\s*(.+)$";
            var m = Regex.Match(raw, pattern);
            if (!m.Success) return false;

            fieldPath = m.Groups[1].Value.Trim();
            var sym = m.Groups[2].Value;
            operand = m.Groups[3].Value.Trim();

            op = sym switch
            {
                ">=" => ComparisonOperator.GreaterThanOrEqual,
                "<=" => ComparisonOperator.LessThanOrEqual,
                "==" or "=" => ComparisonOperator.Equal,
                "!=" => ComparisonOperator.NotEqual,
                ">" => ComparisonOperator.GreaterThan,
                "<" => ComparisonOperator.LessThan,
                _ => ComparisonOperator.Equal
            };

            return true;
        }
    }
}
