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

        public ParamSchema() { }

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
            if (string.IsNullOrWhiteSpace(requiredParam.Name))
                throw new ArgumentException("MainParamDefinition Name is required.", nameof(requiredParam));

            if (requiredParam.DefaultValue == null)
                throw new ArgumentException($"MainParamDefinition '{requiredParam.Name}' must have a default value for compensation.", nameof(requiredParam));

            ValidateConditionRequirements(requiredParam.Name, conditionRequirements);
           
            ValidateLimitations(requiredParam.Name, conditionRequirements, limitations);

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

        /// <summary>
        /// 验证条件要求集合的合法性
        /// </summary>
        private static void ValidateConditionRequirements(string mainParamName, IEnumerable<ConditionRequirement>? conditionRequirements)
        {
            if (conditionRequirements == null) return;

            foreach (var req in conditionRequirements)
            {
                if (string.IsNullOrWhiteSpace(req.FieldName))
                    throw new ArgumentException("ConditionRequirement FieldName cannot be empty.");

                if (req.FieldName == mainParamName)
                    throw new ArgumentException($"Condition field '{req.FieldName}' cannot have the same name as the main param.");
            }
        }

        /// <summary>
        /// 验证限制规则集合的合法性
        /// </summary>
        private static void ValidateLimitations(string mainParamName, IEnumerable<ConditionRequirement>? conditionRequirements, Dictionary<string, ParamLimitation>? limitations)
        {
            if (limitations == null) return;

            var validParamNames = new HashSet<string> { mainParamName };

            if (conditionRequirements != null)
            {
                foreach (var req in conditionRequirements.Where(r => !string.IsNullOrWhiteSpace(r.FieldName)))
                {
                    validParamNames.Add(req.FieldName);
                }
            }

            foreach (var limitationKey in limitations.Keys)
            {
                if (!validParamNames.Contains(limitationKey))
                    throw new ArgumentException($"Limitation key '{limitationKey}' does not match any defined param or condition.");
            }
        }
    }
}
