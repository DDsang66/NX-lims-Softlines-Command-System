using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.src.Domain.Services.Validate
{
    /// <summary>
    /// 检测生成参数是否满足 schema 的要求，若不满足则抛出异常。
    /// 根据 schema 补偿生成参数，若生成参数缺失，则使用 schema 中的默认值进行补偿。
    public class ParamValidateService : IParamValidateService, IScopedDependency
    {

        /*================================结构性验证=====================================*/
        /// <summary>
        /// 对外暴露的验证方法，验证参数是否合规
        /// </summary>
        /// <param name="generated"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        public bool Validate(ParamSet generated, ParamStructure structure) 
        {
            // 1. 基础校验
            ValidateArguments(generated, structure);

            var main = structure.MainParamDefinition;
            var name = main.Name;

            // 2. 核心业务：验证参数是否合规（只验证，不修改状态）
            bool isPresentAndValid = ValidateParamValue(generated, structure, name, out var value);

            return isPresentAndValid;
        }


        /// <summary>
        /// 只做验证（基础参数非空校验）
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void ValidateArguments(ParamSet generated, ParamStructure structure)
        {
            ArgumentNullException.ThrowIfNull(generated);
            ArgumentNullException.ThrowIfNull(structure);

            if (structure.MainParamDefinition == null)
                throw new ArgumentException("Structure's MainParamDefinition is required", nameof(structure));

            if (string.IsNullOrWhiteSpace(structure.MainParamDefinition.Name))
                throw new ArgumentException("Main parameter name is required in structure", nameof(structure));
        }

        /// <summary>
        /// 只做业务合规性验证（不抛出阻断性异常时返回 true，抛出异常代表严重违规）
        /// </summary>
        /// <param name="generated">参数集</param>
        /// <param name="structure">结构定义</param>
        /// <param name="name">参数名</param>
        /// <param name="value">解析出的参数值</param>
        /// <returns>如果参数存在且合规返回 true；如果缺失或为非法 Null 返回 false</returns>
        public bool ValidateParamValue(ParamSet generated, ParamStructure structure, string name, out object value)
        {
            value = null;

            // 尝试获取生成的值
            if (!generated.TryGetValue(name, out var innerValue))
            {
                // 值缺失，需要补偿
                return false;
            }

            // 空值处理：如果不允许为空但却为空，视为不合规（需要补偿默认值，或者直接抛异常取决于业务）
            if (innerValue == null && !structure.MainParamDefinition.IsNullable)
            {
                // 这里建议：如果是严重违规，直接抛异常；如果是数据缺失需要补救，返回 false。
                // 原代码是抛异常，我们保留抛异常的逻辑，但将其隔离在验证方法中
                throw new Exception("Main parameter cannot be null");
            }

            // 合规性校验（依赖接口多态）
            if (structure.Schema.Limitations != null &&
                structure.Schema.Limitations.TryGetValue(name, out var limitation) &&
                limitation is IParamLimitation validator)
            {
                try
                {
                    if (!validator.IsValid(innerValue))
                        throw new Exception("Parameter value violates limitations");
                }
                catch (Exception ex)
                {
                    throw new Exception("Limitation validation failed", ex);
                }
            }

            // 走到这里说明值完全合法
            value = innerValue;

            return true;
        }

        /*=====================================================================*/

        /*============================与测试项目级参数定义比对=================================*/

        /// <summary>
        /// 根据测试项目级参数定义进行验证
        /// </summary>
        /// <param name="param"></param>
        /// <param name="definitions"></param>
        /// <exception cref="ValidationException"></exception>
        public bool ValidateWithItemDefinitions(ParamSet param, IEnumerable<ParamRequireDefinition> definitions)
        {
            if (definitions == null) return true;

            foreach (var pd in definitions)
            {
                if (string.IsNullOrWhiteSpace(pd.ParamName))
                    continue;

                // 1. 验证完整性：参数是否存在
                if (!param.TryGetValue(pd.ParamName, out var existing))
                {
                    return false;
                }

                // 2. 验证非空性：如果定义要求非空，但值为 null
                if (existing == null)
                {
                    return false;
                }

                // 3. 验证类型正确性：如果值不为空，检查类型是否匹配
                if (existing != null && pd.ParamTypeName != null)
                {
                    var expectedType = Type.GetType(pd.ParamTypeName); // 假设定义里有 ParamType 字段
                    if (expectedType != null && !expectedType.IsAssignableFrom(existing.GetType()))
                    {
                        return false;
                    }
                }
            }

            // 遍历完所有定义均符合规则
            return true;
        }
        /*=====================================================================*/
    }  
}
