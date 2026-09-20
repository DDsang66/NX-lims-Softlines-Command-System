using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface IDataSheetModelGenerator:IScopedDependency
    {
        /// <summary>
        /// Generate DataSheetModel from ConditionPool and CheckListItem
        /// </summary>
        /// <param name="pools"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        Task<List<DataSheetModel>> GenerateAsync(List<ConditionPool> pools, CheckListItem item,CancellationToken ct);
    }
}
