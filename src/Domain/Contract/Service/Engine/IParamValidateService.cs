using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine
{
    public interface IParamValidateService:IScopedDependency
    {
        /// <summary>
        /// 对外暴露主方法
        /// </summary>
        /// <param name="generated"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        bool Validate(ParamSet generated, ParamStructure structure);


        /// <summary>
        /// 只做验证（基础参数非空校验）
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        void ValidateArguments(ParamSet generated, ParamStructure structure);

        /// <summary>
        /// 只做业务合规性验证（不抛出阻断性异常时返回 true，抛出异常代表严重违规）
        /// </summary>
        /// <param name="generated">参数集</param>
        /// <param name="structure">结构定义</param>
        /// <param name="name">参数名</param>
        /// <param name="value">解析出的参数值</param>
        /// <returns>如果参数存在且合规返回 true；如果缺失或为非法 Null 返回 false</returns>
        bool ValidateParamValue(ParamSet generated, ParamStructure structure, string name, out object value);

        /// <summary>
        /// 根据测试项目级参数定义进行验证
        /// </summary>
        /// <param name="param"></param>
        /// <param name="definitions"></param>
        /// <exception cref="ValidationException"></exception>
        bool ValidateWithItemDefinitions(ParamSet param, IEnumerable<ParamRequireDefinition> definitions);
    }
}
