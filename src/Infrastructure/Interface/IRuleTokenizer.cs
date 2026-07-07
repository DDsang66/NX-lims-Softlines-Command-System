using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Interface
{
    public interface IRuleTokenizer:IScopedDependency
    {
        List<Token> Tokenize(string text);
    }
}
