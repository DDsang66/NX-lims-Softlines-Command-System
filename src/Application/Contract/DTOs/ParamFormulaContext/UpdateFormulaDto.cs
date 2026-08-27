using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamFormulaContext
{
    public record UpdateFormulaDto
    {
        /// <summary>
        /// 公式ID
        /// </summary>
        public string FormulaId { get; set; } = string.Empty;

        /// <summary>
        /// 公式名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 参数名
        /// </summary>
        public string ParamName { get; set; } = string.Empty;

        /// <summary>
        /// 条件字段集合（参与公式计算的条件字段名列表）
        /// </summary>
        public List<string> ConditionFields { get; set; } = new();

        /// <summary>
        /// 标准族ID（对应领域层的 StandardFamilyId 值对象）
        /// </summary>
        public string StandardFamilyId { get; set; } = string.Empty;

        /// <summary>
        /// 买家ID
        /// </summary>
        public string? BuyerId { get; set; } = string.Empty;

        /// <summary>
        /// 表达式模板（如：${field1} + ${field2}）
        /// </summary>
        public string ExpressionTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 所属引擎层
        /// </summary>
        public string EngineLayerId { get; set; } = string.Empty;
    }
}
