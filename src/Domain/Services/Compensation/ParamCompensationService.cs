using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Services.Compensation
{
    public class ParamCompensationService:IParamCompensationService, IScopedDependency
    {

        /// <summary>
        /// 只做补偿（修改状态）
        /// </summary>
        /// <param name="generated">参数集</param>
        /// <param name="name">参数名</param>
        /// <param name="actualValue">实际值（可能为null）</param>
        /// <param name="defaultValue">默认值</param>
        public void CompensateParamWithStructure(ParamSet generated, string name, object actualValue, object defaultValue)
        {
            // 专注于赋值/补偿逻辑，不关心为什么补偿
            generated.SetValueOrFallback(name, actualValue, defaultValue);
        }

        /// <summary>
        /// 根据参数定义，对缺失的参数进行默认值补偿
        /// </summary>
        /// <param name="param">待补偿的参数集（将被直接修改）</param>
        /// <param name="definitions">参数定义集合</param>
        public void CompensateWithItemDefinitions(ParamSet param, IEnumerable<ParamRequireDefinition> definitions)
        {
            if (definitions == null || param == null) return;

            foreach (var pd in definitions)
            {
                if (string.IsNullOrWhiteSpace(pd.ParamName)) continue;

                // 尝试获取当前值
                param.TryGetValue(pd.ParamName, out var existingValue);

                // 核心补偿逻辑：如果值不存在，或显式为 null，则使用定义中的默认值填充
                // 注意：这里假设即使 Key 存在但 Value 为 null，也需要补偿。
                // 如果你的业务是：只要 Key 存在就不补偿，请把条件改为：if (existingValue == null) 
                if (existingValue == null)
                {
                    // 调用你原有的赋值逻辑，如果 DefaultValue 也为 null，则根据 SetValueOrFallback 的内部逻辑处理
                    param.SetValueOrFallback(pd.ParamName, null, pd.ParamDefaultValue);
                }
            }
        }

    }
}
