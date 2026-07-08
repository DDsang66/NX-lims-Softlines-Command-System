using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine
{
    public interface ITokenizer: IScopedDependency
    {
        /// <summary>
        /// 将文本解析为 Token 列表
        /// </summary>
        /// <param name="text">原始规则文本</param>
        /// <returns>Token 序列</returns>
        /// <exception cref="ArgumentException">文本为空或格式错误</exception>
        IReadOnlyList<Token> Tokenize(string text);
    }
}
