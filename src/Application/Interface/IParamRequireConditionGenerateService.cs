using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface IParamRequireConditionGenerateService:IScopedDependency
    {
        /// <summary>
        /// 生成condition必填结构
        /// </summary>
        /// <param name="checklistid"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<IDictionary<string, object?>>> GenerateRequiredConditionsAsync(CheckListId checklistid, CancellationToken ct);
    }
}
