using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
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
        public ParamSet ApplyCompensation(ParamSet generated, ParamSchema schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (generated == null) throw new ArgumentNullException(nameof(generated));

            var result = new ParamSet();
            var main = schema.RequiredParam ?? throw new ArgumentException("Schema.RequiredParam is required", nameof(schema));
            var name = main.Name;

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Main parameter name is required in schema", nameof(schema));

            if (generated.TryGetValue(name, out var value))
            {
                // 空值处理：如果值为 null 且参数不可空，则认为越界
                if (value == null && !main.IsNullable)
                    throw new Exception(name);

                // 若存在 limitation，优先使用其 IsValid 校验（传入回退类型）
                if (schema.Limitations != null && schema.Limitations.TryGetValue(name, out var lim) && lim != null)
                {
                    var ok = true;
                    try
                    {
                        ok = lim.IsValid(value, main.ValueType);
                    }
                    catch
                    {
                        // 如果 limitation 校验抛异常，视为校验失败
                        ok = false;
                    }

                    if (!ok)
                        throw new Exception(name);
                }

                result.Add(name, value);
            }
            else
            {
                // 补偿：使用默认值（由 ParamDefinition 提供）
                result.Add(name, main.DefaultValue);
            }

            return result;
        }
    }
}
