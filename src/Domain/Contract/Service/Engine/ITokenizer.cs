using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine
{
    public interface ITokenizer: IScopedDependency
    {
        /// <summary>
        /// 将输入文本转换为词法单元列表
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>词法单元列表</returns>
        List<Token> Tokenize(string text);

        /// <summary>
        /// 验证词法单元列表的有效性
        /// </summary>
        /// <param name="tokens">词法单元列表</param>
        /// <returns>验证结果</returns>
        bool ValidateTokens(List<Token> tokens);
    }
}
