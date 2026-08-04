using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext
{
    public sealed class Formula : AggregateRoot<FormulaId,string>
    {
        private readonly List<ParamStructureId?> _paramStructureIds = new();

        private readonly List<StandardFamilyId?> _standardFamilyIds = new();
        /// <summary>
        /// 公式ID
        /// </summary>
        //public FormulaId Id { get; private set; }

        /// <summary>
        /// 参数结构 Id
        /// </summary>
        public IReadOnlyCollection<ParamStructureId?> ParamStructureIds => _paramStructureIds.AsReadOnly();

        /// <summary>
        /// 标准族 Id
        /// </summary>
        public IReadOnlyCollection<StandardFamilyId?> StandardFamilyIds => _standardFamilyIds.AsReadOnly();

        /// <summary>
        /// 公式名称
        /// </summary>
        public string Name { get; private set; } = string.Empty;  // "BallastDerivation"

        /// <summary>
        /// 生成参数名
        /// </summary>
        public string ParamName { get; private set; } = string.Empty;  // 生成的参数名 "Ballast"

        /// <summary>
        /// 条件字段
        /// </summary>
        public List<string> ConditionFields { get; private set; } = new(); // ["FiberDominantType", "BuyerSpecified"]等具体语义的字段名(不可再切割)

        /// <summary>
        /// 公式模板
        /// 模板格式（建议，必须严格遵守）：
        /// SlotName{field1,field2,...} + SlotName2{f3,f4} -> Result
        /// 例如：Equal{MachineType,Temperature,WashingProcess} + Comparer{FiberContent.Polyester,Weight} -> Result
        /// </summary>
        public string ExpressionTemplate { get; private set; } = string.Empty; // "FiberDominantType + BuyerSpecified ->Ballst" 范式样本

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; private set; }  // 版本号

        /// <summary>
        /// 生效日期
        /// </summary>
        public DateTime EffectiveDate { get; private set; }  // 生效日期

        /// <summary>
        /// 公式是否启用
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 创建 Formula 聚合根的实例（工厂方法，仅在内存中创建并保证不变式）
        /// 持久化请通过 IFormulaRepository 在应用层完成（例如 repository.Add(formula) 后提交事务）
        /// </summary>
        public static Formula Create(
            FormulaId id,
            string name,
            string paramName,
            IEnumerable<StandardFamilyId?> standardFamilyIds,
            IEnumerable<ParamStructureId?> paramStructureIds,
            IEnumerable<string> conditionFields,
            string expressionTemplate,
            string? description = null)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name required", nameof(name));
            if (string.IsNullOrWhiteSpace(paramName))
                throw new ArgumentException("ParamName required", nameof(paramName));
            if (conditionFields == null)
                throw new ArgumentNullException(nameof(conditionFields));

            // 规范化、去重并校验字段名
            var fields = conditionFields
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (fields.Count == 0)
                throw new ArgumentException("At least one condition field is required", nameof(conditionFields));

            var f = new Formula
            {
                Id = id,
                Name = name.Trim(),
                ParamName = paramName.Trim(),
                ConditionFields = fields,
                ExpressionTemplate = expressionTemplate ?? string.Empty,
                Description = description?.Trim(),
                IsActive = false,
                Version = 1,
                EffectiveDate = DateTime.UtcNow,
            };

            // 3. 初始化 StandardFamilyIds 集合
            if (standardFamilyIds != null)
            {
                foreach (var familyId in standardFamilyIds.Where(fid => fid != null))
                {
                    f._standardFamilyIds.Add(familyId);
                }
            }

            if (paramStructureIds != null) 
            {
                foreach (var paramStructureId in paramStructureIds.Where(psid => psid != null))
                {
                    f._paramStructureIds.Add(paramStructureId);
                }
            }

            return f;
        }


        /// <summary>
        /// 根据持久化数据重新构建 Formula 聚合根的实例（工厂方法，仅在内存中创建并保证不变式）
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="paramName"></param>
        /// <param name="conditionFields"></param>
        /// <param name="familyId"></param>
        /// <param name="expressionTemplate"></param>
        /// <param name="description"></param>
        /// <param name="version"></param>
        /// <param name="isActive"></param>
        /// <param name="effectiveDate"></param>
        /// <returns></returns>
        internal static Formula Reconstitute(
           FormulaId id,
           string name,
           string paramName,
           IEnumerable<string> conditionFields,
           IEnumerable<StandardFamilyId?> standardFamilyIds,
           IEnumerable<ParamStructureId?> paramStructureIds,
           string expressionTemplate,
           int version,
           bool isActive,
           DateTime effectiveDate,
           string? description = null)
        {
            var f = new Formula
            {
                Id = id,
                Name = name,
                ParamName = paramName,
                ConditionFields = conditionFields.ToList(),
                ExpressionTemplate = expressionTemplate,
                Description = description,
                Version = version,
                IsActive = isActive,
                EffectiveDate = effectiveDate
            };

            // 5. 重建 StandardFamilyIds 集合
            if (standardFamilyIds != null)
            {
                foreach (var familyId in standardFamilyIds.Where(fid => fid != null))
                {
                    f._standardFamilyIds.Add(familyId);
                }
            }

            if (paramStructureIds != null)
            {
                foreach (var paramStructureId in paramStructureIds.Where(psid => psid != null))
                {
                    f._paramStructureIds.Add(paramStructureId);
                }
            }

            return f;
        }

        /// <summary>
        /// 更新公式的基础信息及表达式。如果公式处于激活状态，将重新进行语法校验。
        /// </summary>
        public void Update(
            string name,
            string paramName,
            IEnumerable<string> conditionFields,
            string expressionTemplate,
            string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(paramName))
                throw new ArgumentException("ParamName cannot be empty.", nameof(paramName));
            if (conditionFields == null)
                throw new ArgumentNullException(nameof(conditionFields));

            // 规范化、去重并校验字段名
            var fields = conditionFields
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (fields.Count == 0)
                throw new ArgumentException("At least one condition field is required.", nameof(conditionFields));

            Name = name.Trim();
            ParamName = paramName.Trim();
            ConditionFields = fields;
            ExpressionTemplate = expressionTemplate ?? string.Empty;
            Description = description?.Trim() ?? string.Empty;

            // 如果当前公式是激活状态，修改核心数据后必须重新校验不变式
            if (IsActive)
            {
                ValidateExpression();
            }

            // 更新版本号和生效时间
            Version++;
            EffectiveDate = DateTime.UtcNow;
        }

        /// <summary>
        /// 返回公式声明的原子条件字段名（供前置验证）
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> RequiredConditions() => ConditionFields.AsReadOnly();

        /// <summary>
        /// 关闭公式，使其不参与计算。通常在不需要使用某个公式时，可以调用此方法来禁用公式的计算功能。
        /// </summary>
        public void Deactivate() => IsActive = false;

        /// <summary>
        /// 激活公式，使其参与计算。
        /// 通常在创建或修改公式后需要调用此方法来启用公式的计算功能。
        /// </summary>
        public void Activate()
        {
            // 1. 必须有归属
            if (_standardFamilyIds.Count == 0)
                throw new InvalidOperationException("Formula must be attached to at least one StandardFamily before activation");

            // 2. 必须有表达式模板
            if (string.IsNullOrWhiteSpace(ExpressionTemplate))
                throw new InvalidOperationException("ExpressionTemplate is required for activation");

            // 3. 必须有条件字段
            if (ConditionFields == null || ConditionFields.Count == 0)
                throw new InvalidOperationException("At least one condition field is required for activation");

            // 4. 校验表达式模板语法
            ValidateExpression();

            IsActive = true;
        }


        /// <summary>
        /// 基于 token 列表校验表达式模板：
        /// - 必须包含且仅包含一个推导符（RangeOperator token）
        /// - 所有 ConditionFields 必须以完整标识符出现在 token 列表中（忽略大小写）
        /// 返回 Result.Ok() 表示通过，否则返回 Result.Fail(...) 并附带缺失字段信息
        /// </summary>
        /// <param name="tokens">由上层 Tokenizer 生成的 token 列表</param>
        public Result ValidateExpressionTokens(IReadOnlyList<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return Result.Fail("ExpressionTemplate tokens are empty");

            // 推导符数量检查
            var arrowCount = tokens.Count(t => t.Type == TokenType.RangeOperator);
            if (arrowCount == 0)
                return Result.Fail("ExpressionTemplate must contain a derivation operator (→, ->, =>, to, ~).");
            if (arrowCount > 1)
                return Result.Fail("ExpressionTemplate must contain only one derivation operator.");

            // 检查每个 ConditionField 是否以完整标识符出现在 tokens 中
            var missing = new List<string>();
            if (ConditionFields != null)
            {
                foreach (var field in ConditionFields.Where(f => !string.IsNullOrWhiteSpace(f)))
                {
                    var exists = tokens.Any(t =>
                        t.Type == TokenType.Identifier &&
                        string.Equals(t.Value, field, StringComparison.OrdinalIgnoreCase));
                    if (!exists) missing.Add(field);
                }
            }

            if (missing.Any())
                return Result.Fail("Some condition fields are not present in the expression template", details: missing);

            return Result.Ok();
        }



        /// <summary>
        /// 校验表达式模板语法（支持两种左侧语法）：
        /// 1) 槽位语法：SlotName{field1,field2}+Slot2{...} -> Result
        ///    - 严格解析每个槽并验证 ConditionFields 是否全部出现在槽定义中
        /// 2) 旧式兼容语法：直接在模板中包含字段标识符（原有行为）
        /// 
        /// 语义/约束：
        /// - 仅允许一个推导符（->, =>, →, ~ 或 独立单词 "to"）
        /// - 若采用槽位语法，必须保证每个槽内部字段非空且由逗号分隔
        /// - 若模板为旧式语法，则保持原有对 ConditionFields 出现性的验证
        /// </summary>
        public void ValidateExpression()
        {
            if (string.IsNullOrWhiteSpace(ExpressionTemplate))
                throw new InvalidOperationException("ExpressionTemplate is required");

            // 寻找推导符（支持符号和单词 "to"）
            var derivationOperators = new[] { "→", "->", "=>", "~", "to" };
            var found = new List<(string op, int index, int length)>();

            foreach (var op in derivationOperators)
            {
                if (string.Equals(op, "to", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (Match m in Regex.Matches(ExpressionTemplate, @"\bto\b", RegexOptions.IgnoreCase))
                    {
                        found.Add((op, m.Index, m.Length));
                    }
                }
                else
                {
                    var idx = ExpressionTemplate.IndexOf(op, StringComparison.Ordinal);
                    if (idx >= 0) found.Add((op, idx, op.Length));
                }
            }

            if (found.Count == 0)
                throw new InvalidOperationException("ExpressionTemplate must contain a derivation operator like '->','=>','to','~' or '→'.");
            if (found.Count > 1)
                throw new InvalidOperationException("ExpressionTemplate must contain only one derivation operator.");

            var opFound = found.OrderBy(f => f.index).First();
            var left = ExpressionTemplate.Substring(0, opFound.index).Trim();
            var right = ExpressionTemplate.Substring(opFound.index + opFound.length).Trim();

            if (string.IsNullOrWhiteSpace(left))
                throw new InvalidOperationException("ExpressionTemplate left side is empty.");
            if (string.IsNullOrWhiteSpace(right))
                throw new InvalidOperationException("ExpressionTemplate right side (result) is empty.");

            // 如果使用槽位语法（识别到大括号），解析槽位
            if (left.Contains("{") && left.Contains("}"))
            {
                var slotFields = new Dictionary<SlotType, HashSet<string>>();

                var parts = left.Split('+');
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

                    // 用枚举校验槽名（忽略大小写），并给出可选项提示
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

                    if (!slotFields.TryGetValue(slotType, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        slotFields[slotType] = set;
                    }

                    foreach (var f in fields)
                    {
                        set.Add(f); // HashSet 自动去重（忽略大小写）
                    }
                }

                // 验证所有 ConditionFields 都出现在任意槽定义中（按忽略大小写）
                var allSlotFields = slotFields.Values.SelectMany(s => s).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var missing = ConditionFields
                    .Where(cf => !allSlotFields.Contains(cf ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                    .ToList();
                if (missing.Any())
                    throw new InvalidOperationException($"Some condition fields are not present in the slot definitions: {string.Join(", ", missing)}");
            }
            else
            {
                // 兼容旧式模板：检查每个 ConditionField 是否以完整标识符出现在模板中
                foreach (var field in ConditionFields.Where(f => !string.IsNullOrWhiteSpace(f)))
                {
                    var escaped = Regex.Escape(field.Trim());
                    var pattern = $@"\b{escaped}\b";
                    if (!Regex.IsMatch(ExpressionTemplate ?? string.Empty, pattern, RegexOptions.IgnoreCase))
                    {
                        throw new InvalidOperationException($"Condition field '{field}' is not present in ExpressionTemplate.");
                    }
                }
            }
        }

        /// <summary>
        /// 添加关联的标准族
        /// </summary>
        public void AddStandardFamily(StandardFamilyId familyId)
        {
            if (familyId == null) throw new ArgumentNullException(nameof(familyId));
            if (!_standardFamilyIds.Contains(familyId)) // 保证幂等性/去重
            {
                _standardFamilyIds.Add(familyId);
            }
        }

        /// <summary>
        /// 移除关联的标准族
        /// </summary>
        public void RemoveStandardFamily(StandardFamilyId familyId)
        {
            if (familyId == null) throw new ArgumentNullException(nameof(familyId));
            _standardFamilyIds.Remove(familyId);
        }

        /// <summary>
        /// 添加关联的参数结构
        /// </summary>
        public void AddParamStructure(ParamStructureId paramStructureId)
        {
            if (paramStructureId == null) throw new ArgumentNullException(nameof(paramStructureId));
            if (!_paramStructureIds.Contains(paramStructureId))
            {
                _paramStructureIds.Add(paramStructureId);
            }
        }

        /// <summary>
        /// 移除关联的参数结构
        /// </summary>
        public void RemoveParamStructure(ParamStructureId paramStructureId)
        {
            if (paramStructureId == null) throw new ArgumentNullException(nameof(paramStructureId));
            _paramStructureIds.Remove(paramStructureId);
        }

    }
}
