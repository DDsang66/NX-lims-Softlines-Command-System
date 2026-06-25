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
    }
}
