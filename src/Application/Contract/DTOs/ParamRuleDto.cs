namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    namespace NX_lims_Softlines_Command_System.src.Application.ParamEngineContext.Dtos
    {
        /// <summary>
        /// 参数规则响应DTO
        /// </summary>
        public class ParamRuleDto
        {
            /// <summary>
            /// 规则ID
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// 所属公式ID
            /// </summary>
            public string FormulaId { get; set; }

            /// <summary>
            /// 参数名
            /// </summary>
            public string ParamName { get; set; }

            /// <summary>
            /// 优先级
            /// </summary>
            public int Priority { get; set; }

            /// <summary>
            /// 是否激活
            /// </summary>
            public bool IsActive { get; set; }

            /// <summary>
            /// 等值匹配条件
            /// </summary>
            public List<EqualMatchDto> EqualMatches { get; set; } = new();

            /// <summary>
            /// 比较匹配条件
            /// </summary>
            public List<ComparisonMatchDto> ComparisonMatches { get; set; } = new();

            /// <summary>
            /// 集合匹配条件
            /// </summary>
            public List<InMatchDto> InMatches { get; set; } = new();

            /// <summary>
            /// 复合条件
            /// </summary>
            public List<CompositeConditionDto> CompositeMatches { get; set; } = new();

            /// <summary>
            /// 结果值
            /// </summary>
            public object? ResultValue { get; set; }

            /// <summary>
            /// 结果备注
            /// </summary>
            public string? ResultNotes { get; set; }
        }

        /// <summary>
        /// 等值匹配DTO
        /// </summary>
        public class EqualMatchDto
        {
            /// <summary>
            /// 字段名
            /// </summary>
            public string Field { get; set; }

            /// <summary>
            /// 期望值
            /// </summary>
            public object Value { get; set; }
        }

        /// <summary>
        /// 比较匹配DTO
        /// </summary>
        public class ComparisonMatchDto
        {
            /// <summary>
            /// 字段路径
            /// </summary>
            public string FieldPath { get; set; }

            /// <summary>
            /// 运算符
            /// </summary>
            public string Operator { get; set; }

            /// <summary>
            /// 期望值
            /// </summary>
            public object ExpectedValue { get; set; }
        }

        /// <summary>
        /// 集合匹配DTO
        /// </summary>
        public class InMatchDto
        {
            /// <summary>
            /// 字段名
            /// </summary>
            public string Field { get; set; }

            /// <summary>
            /// 允许的值列表
            /// </summary>
            public List<object> Values { get; set; } = new();
        }

        /// <summary>
        /// 复合条件DTO
        /// </summary>
        public class CompositeConditionDto
        {
            /// <summary>
            /// 逻辑运算符（And/Or/Not）
            /// </summary>
            public string Logic { get; set; }

            /// <summary>
            /// 字段名列表
            /// </summary>
            public List<string> FieldNames { get; set; } = new();

            /// <summary>
            /// 子比较条件列表
            /// </summary>
            public List<ComparisonMatchDto> SubConditions { get; set; } = new();
        }
    }
}
