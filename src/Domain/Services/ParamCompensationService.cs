using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    public class ParamCompensationService : IParamCompensationService,IScopedDependency
    {
        /// <summary>
        /// 检测生成参数是否满足 schema 的要求，若不满足则抛出异常。
        /// 根据 schema 补偿生成参数，若生成参数缺失，则使用 schema 中的默认值进行补偿。
        /// </summary>
        public ParamSet ConformToStructure(ParamSet generated, ParamStructure structure)
        {
            // 1. 参数校验
            ArgumentNullException.ThrowIfNull(generated);
            ArgumentNullException.ThrowIfNull(structure);

            var main = structure.MainParamDefinition;
            if (main == null)
                throw new ArgumentException("Structure's MainParamDefinition is required", nameof(structure));

            var name = main.Name;
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Main parameter name is required in structure", nameof(structure));

            // 2. 尝试获取生成的值
            if (generated.TryGetValue(name, out var value))
            {
                // 2.1 空值处理
                if (value == null && !main.IsNullable)
                    throw new Exception("Main parameter cannot be null");

                // 2.2 合规性校验（依赖接口多态，避免反射）
                if (structure.Schema.Limitations != null &&
                    structure.Schema.Limitations.TryGetValue(name, out var limitation) &&
                    limitation is IParamLimitation validator)
                {
                    try
                    {
                        if (!validator.IsValid(value))
                            throw new Exception("Parameter value violates limitations");
                    }
                    catch (Exception ex)
                    {
                        // 将校验过程中的异常包装为领域异常，保留原始错误信息
                        throw new Exception("Limitation validation failed", ex);
                    }
                }

                // 值合法，写入结果
                generated.SetValueOrFallback(name, value, main.DefaultValue);
            }
            else
            {
                // 3. 缺失补偿：直接使用默认值
                generated.SetValueOrFallback(name, null, main.DefaultValue);
            }

            return generated;
        }
    }
}
