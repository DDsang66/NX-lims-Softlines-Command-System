using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine
{
    /// <summary>
    /// 规则解析器契约
    /// 将 Token 序列解析为结构化规则数据
    /// </summary>
    public interface IParser:IScopedDependency
    {
        /// <summary>
        /// 解析规则文本为结构化数据
        /// </summary>
        /// <param name="tokens">词法单元序列</param>
        /// <param name="formula">公式范式定义</param>
        /// <returns>解析结果</returns>
        ParsedRule Parse(IReadOnlyList<Token> tokens, Formula formula);
    }
}
