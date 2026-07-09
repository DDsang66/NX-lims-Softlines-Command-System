namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj
{
    public class ParamSchema
    { 
        // 1. 要生成的参数定义
        public ParamDefinition RequiredParam { get; set; } = new ParamDefinition();

        // 2. 需要的条件定义（与公式对应）
        public List<ConditionRequirement> ConditionRequirements { get; set; } = new List<ConditionRequirement>();

        // 3. 参数取值限制集合： key = 参数名（通常为 RequiredParam.Name），value = 限制定义
        // 注意：ParamLimitation 可选择性声明 ValueType；
        public Dictionary<string, ParamLimitation> Limitations { get; set; } = new Dictionary<string, ParamLimitation>();

        private ParamSchema() { }

        /// <summary>
        /// 创建一个 ParamSchema 实例
        /// </summary>
        /// <param name="requiredParam"></param>
        /// <param name="conditionRequirements"></param>
        /// <param name="limitations"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ParamSchema Create(
            ParamDefinition requiredParam,
            IEnumerable<ConditionRequirement>? conditionRequirements = null,
            Dictionary<string, ParamLimitation>? limitations = null)
        {
            if (requiredParam == null)
                throw new ArgumentNullException(nameof(requiredParam));

            return new ParamSchema
            {
                RequiredParam = requiredParam,
                ConditionRequirements = conditionRequirements?.ToList() ?? new List<ConditionRequirement>(),
                Limitations = limitations ?? new Dictionary<string, ParamLimitation>()
            };
        }

        /// <summary>
        /// Reconstitute 供仓储层/Mapster 从数据库重建
        /// </summary>
        /// <param name="requiredParam"></param>
        /// <param name="conditionRequirements"></param>
        /// <param name="limitations"></param>
        /// <returns></returns>
        internal static ParamSchema Reconstitute(
            ParamDefinition requiredParam,
            List<ConditionRequirement>? conditionRequirements = null,
            Dictionary<string, ParamLimitation>? limitations = null)
        {
            return new ParamSchema
            {
                RequiredParam = requiredParam,
                ConditionRequirements = conditionRequirements ?? new List<ConditionRequirement>(),
                Limitations = limitations ?? new Dictionary<string, ParamLimitation>()
            };
        }



    }
}
