using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface IParamGenerationUseCaseService:IScopedDependency
    {
        /// <summary>
        /// 为 CheckList 的某个 Item 生成参数
        /// </summary>
        /// <param name="checkListItem"></param>
        /// <param name="pool"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<ParamSet>> GenerateForCheckListItemAsync(CheckListItem checkListItem, ConditionPool pool, CancellationToken ct);

        /// <summary>
        /// 为某个 ParamStructure 生成参数
        /// </summary>
        /// <param name="structureId"></param>
        /// <param name="pool"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<ParamSet>> GenerateForStructureAsync(ParamStructureId structureId, ConditionPool pool, CancellationToken ct);
    }
}
