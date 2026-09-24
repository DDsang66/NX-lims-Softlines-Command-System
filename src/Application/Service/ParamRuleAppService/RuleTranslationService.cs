using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ParamRuleAppService
{
    ///// <summary>
    ///// 语言规则翻译服务
    ///// 支持文本结构、json结构规则
    ///// </summary>
    //public class RuleTranslationService: IRuleTranslationService,IScopedDependency
    //{
    //    private readonly IConditionPatternBuilder _patternBuilder;
    //    private readonly ITokenizer _tokenizer;
    //    private readonly IParser _parser;

    //    public RuleTranslationService(
    //        IConditionPatternBuilder patternBuilder,
    //        ITokenizer tokenizer,
    //        IParser parser
    //        )
    //    {
    //        _patternBuilder = patternBuilder;
    //        _tokenizer = tokenizer;
    //        _parser = parser;
    //    }

    //    /// <summary>
    //    /// 根据DTO创建条件模式
    //    /// </summary>
    //    /// <param name="request"></param>
    //    /// <returns></returns>
    //    public ConditionPattern PatternTranslateFromDto(CreateParamRuleRequest request,CancellationToken ct)
    //    {
    //        // 处理DTO到领域对象的转换
    //        foreach (var match in request.EqualMatches)
    //        {
    //            _patternBuilder.AddEqual(match.Field, match.Value);
    //        }

    //        foreach (var match in request.ComparisonMatches)
    //        {
    //            _patternBuilder.AddComparison(
    //                match.FieldPath,
    //                ParseComparisonOperator(match.Operator),
    //                match.ExpectedValue);
    //        }

    //        foreach (var match in request.ComparisonMatches)
    //        {
    //            _patternBuilder.AddComparison(
    //                match.FieldPath,
    //                ParseComparisonOperator(match.Operator),
    //                match.ExpectedValue);
    //        }

    //        foreach (var match in request.InMatches)
    //        {
    //            _patternBuilder.AddIn(match.Field, match.Values);
    //        }

    //        foreach (var match in request.CompositeMatches)
    //        {
    //            var composite = new CompositeCondition
    //            {
    //                Logic = ParseLogicalOperator(match.Logic),
    //                FieldNames = match.FieldNames,
    //                SubConditions = match.SubConditions.Select(sc => new ComparisonCondition
    //                {
    //                    FieldPath = sc.FieldPath,
    //                    Operator = ParseComparisonOperator(sc.Operator),
    //                    ExpectedValue = sc.ExpectedValue
    //                }).ToList()
    //            };
    //            _patternBuilder.AddComposite(composite);
    //        }

    //        return _patternBuilder.Build();
    //    }


    //    /// <summary>
    //    /// 根据自然语言文本创建条件模式
    //    /// </summary>
    //    /// <param name="text"></param>
    //    /// <param name="formula"></param>
    //    /// <param name="ct"></param>
    //    /// <returns></returns>
    //    public (ConditionPattern pattern,ParamValue paramValue) ParseFromNaturalLanguageText(string text, Formula formula, CancellationToken ct)
    //    {
    //        // 处理文本到领域对象的转换
    //        var tokens = _tokenizer.Tokenize(text);

    //        var parsedRule = _parser.Parse(text, tokens, formula);

    //        // JSON 反序列化为 ConditionPattern
    //        // JSON 反序列化为 ConditionPattern（做空检查并使用合适的选项）
    //        var json = parsedRule.ConditionPatternJson;
    //        if (json == null)
    //            throw new InvalidOperationException("解析失败：ConditionPatternJson 为 null。请检查 Parser 的输出。");

    //        var options = new System.Text.Json.JsonSerializerOptions
    //        {
    //            PropertyNameCaseInsensitive = true
    //        };
    //        // 允许把枚举的字符串名反序列化为枚举值（例如 "GreaterThanOrEqual" -> ComparisonOperator.GreaterThanOrEqual）
    //        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

    //        var pattern = json.Deserialize<ConditionPattern>(options)
    //                      ?? throw new InvalidOperationException("反序列化失败：无法将 ConditionPatternJson 转换为 ConditionPattern。");

    //        var paramValue = new ParamValue(parsedRule.ResultValue);

    //        return (pattern, paramValue);
    //    }

    //    /// <summary>
    //    /// 根据JSON创建条件模式
    //    /// </summary>
    //    /// <param name="op"></param>
    //    /// <returns></returns>
    //    /// <exception cref="ArgumentException"></exception>
    //    private ComparisonOperator ParseComparisonOperator(string op)
    //    {
    //        return op switch
    //        {
    //            "Equal" => ComparisonOperator.Equal,
    //            "NotEqual" => ComparisonOperator.NotEqual,
    //            "GreaterThan" => ComparisonOperator.GreaterThan,
    //            "GreaterThanOrEqual" => ComparisonOperator.GreaterThanOrEqual,
    //            "LessThan" => ComparisonOperator.LessThan,
    //            "LessThanOrEqual" => ComparisonOperator.LessThanOrEqual,
    //            _ => throw new ArgumentException($"Unknown operator: {op}")
    //        };
    //    }

    //    /// <summary>
    //    /// 根据JSON创建逻辑运算符
    //    /// </summary>
    //    /// <param name="logic"></param>
    //    /// <returns></returns>
    //    /// <exception cref="ArgumentException"></exception>
    //    private LogicalOperator ParseLogicalOperator(string logic)
    //    {
    //        return logic switch
    //        {
    //            "And" => LogicalOperator.And,
    //            "Or" => LogicalOperator.Or,
    //            "Not" => LogicalOperator.Not,
    //            _ => throw new ArgumentException($"Unknown logic: {logic}")
    //        };
    //    }
    //}

    /// <summary>
    /// 规则翻译服务
    /// 
    /// 职责：
    /// 1. 将 DTO 转换为领域对象（ConditionPattern + ParamValue）
    /// 2. 将自然语言文本解析为领域对象
    /// 3. 将 JSON 结构解析为领域对象
    /// 
    /// 设计说明：
    /// - 本服务是无状态的（Stateless），每次调用都创建新的 Builder
    /// - 所有解析入口都做参数校验，避免 NRE
    /// - 枚举转换统一走 ParseXxx 方法，避免魔法字符串散落
    /// </summary>
    public class RuleTranslationService : IRuleTranslationService, IScopedDependency
    {
        private readonly Func<IConditionPatternBuilder> _patternBuilderFactory;
        private readonly ITokenizer _tokenizer;
        private readonly IParser _parser;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public RuleTranslationService(
            Func<IConditionPatternBuilder> patternBuilderFactory,
            ITokenizer tokenizer,
            IParser parser)
        {
            _patternBuilderFactory = patternBuilderFactory ?? throw new ArgumentNullException(nameof(patternBuilderFactory));
            _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        #region DTO → ConditionPattern

        /// <summary>
        /// 根据 DTO 创建条件模式
        /// 
        /// 支持全部 5 种匹配模式：
        /// - EqualMatches
        /// - ComparisonMatches
        /// - InMatches
        /// - CompositeMatches（含递归 Children）
        /// - AssignMatches
        /// </summary>
        public ConditionPattern PatternTranslateFromDto(
            CreateParamRuleRequest request,
            CancellationToken ct)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ct.ThrowIfCancellationRequested();

            // 每次调用创建新的 Builder，避免状态累积
            var builder = _patternBuilderFactory();

            // 1. EqualMatches
            if (request.EqualMatches != null)
            {
                foreach (var match in request.EqualMatches)
                {
                    if (string.IsNullOrWhiteSpace(match.Field))
                        throw new ArgumentException("EqualMatch.Field 不能为空", nameof(request));

                    builder.AddEqual(match.Field, match.Value);
                }
            }

            // 2. ComparisonMatches
            if (request.ComparisonMatches != null)
            {
                foreach (var match in request.ComparisonMatches)
                {
                    if (string.IsNullOrWhiteSpace(match.FieldPath))
                        throw new ArgumentException("ComparisonMatch.FieldPath 不能为空", nameof(request));

                    builder.AddComparison(
                        match.FieldPath,
                        ParseComparisonOperator(match.Operator),
                        match.ExpectedValue);
                }
            }

            // 3. InMatches
            if (request.InMatches != null)
            {
                foreach (var match in request.InMatches)
                {
                    if (string.IsNullOrWhiteSpace(match.Field))
                        throw new ArgumentException("InMatch.Field 不能为空", nameof(request));

                    builder.AddIn(match.Field, match.Values ?? Enumerable.Empty<object?>());
                }
            }

            // 4. CompositeMatches（支持递归 Children）
            if (request.CompositeMatches != null)
            {
                foreach (var match in request.CompositeMatches)
                {
                    var composite = BuildCompositeCondition(match);
                    builder.AddComposite(composite);
                }
            }

            // 5. AssignMatches（新增）
            if (request.AssignMatches != null)
            {
                foreach (var match in request.AssignMatches)
                {
                    if (string.IsNullOrWhiteSpace(match.SourceFieldPath))
                        throw new ArgumentException("AssignMatch.SourceFieldPath 不能为空", nameof(request));

                    builder.AddAssign(
                        match.SourceFieldPath,
                        match.IsRequired,
                        match.DefaultValue);
                }
            }

            ct.ThrowIfCancellationRequested();

            return builder.Build();
        }

        /// <summary>
        /// 递归构建 CompositeCondition（支持任意深度的 Children）
        /// </summary>
        private CompositeCondition BuildCompositeCondition(CompositeConditionDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var composite = new CompositeCondition
            {
                Logic = ParseLogicalOperator(dto.Logic),
                FieldNames = dto.FieldNames?.ToList(),

                SubConditions = dto.SubConditions?
                    .Select(sc => new ComparisonCondition
                    {
                        FieldPath = sc.FieldPath,
                        Operator = ParseComparisonOperator(sc.Operator),
                        ExpectedValue = sc.ExpectedValue
                    })
                    .ToList(),

                // 递归处理 Children
                Children = dto.Children?
                    .Select(BuildCompositeCondition)
                    .ToList()
            };

            return composite;
        }

        #endregion

        #region 自然语言文本 → 领域对象

        /// <summary>
        /// 根据自然语言文本创建条件模式
        /// </summary>
        public (ConditionPattern pattern, ParamValue paramValue) ParseFromNaturalLanguageText(
            string text,
            Formula formula,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("文本不能为空", nameof(text));
            if (formula == null)
                throw new ArgumentNullException(nameof(formula));

            ct.ThrowIfCancellationRequested();

            // 1. 从 Formula 创建解析上下文
            var context = formula.CreateParseContext();

            // 2. 词法分析
            var tokens = _tokenizer.Tokenize(text);

            ct.ThrowIfCancellationRequested();

            // 3. 语法分析 + 语义分析（直接返回强类型）
            var parsedRule = _parser.Parse(text, tokens, formula)
                ?? throw new InvalidOperationException("解析失败：Parser 返回 null");

            // 4. 校验解析结果（可选）
            var validation = formula.ValidateParseResult(parsedRule.Pattern);

            if (!validation.IsSuccess)
                throw new ArgumentException($"解析结果与公式 '{formula.Name}' 不匹配");

            // 5. 直接返回强类型结果
            return (parsedRule.Pattern, parsedRule.Result);

        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 解析比较运算符（字符串 → 枚举）
        /// </summary>
        private static ComparisonOperator ParseComparisonOperator(string op)
        {
            if (string.IsNullOrWhiteSpace(op))
                throw new ArgumentException("比较运算符不能为空", nameof(op));

            // 使用 Enum.TryParse 支持多种命名风格
            if (Enum.TryParse<ComparisonOperator>(op, ignoreCase: true, out var result))
                return result;

            throw new ArgumentException($"未知的比较运算符: {op}", nameof(op));
        }

        /// <summary>
        /// 解析逻辑运算符（字符串 → 枚举）
        /// </summary>
        private static LogicalOperator ParseLogicalOperator(string logic)
        {
            if (string.IsNullOrWhiteSpace(logic))
                throw new ArgumentException("逻辑运算符不能为空", nameof(logic));

            if (Enum.TryParse<LogicalOperator>(logic, ignoreCase: true, out var result))
                return result;

            throw new ArgumentException($"未知的逻辑运算符: {logic}", nameof(logic));
        }

        #endregion
    }
}
