using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TestItemContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.TestItemContext
{
    public interface ITestItemAppService:IScopedDependency
    {
        /// <summary>
        /// 创建测试项目
        /// </summary>
        /// <returns></returns>
        Task<Result> AddTestItemAsync(AddTestItemDto dto,CancellationToken ct);

        /// <summary>
        /// 更新测试项目
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> UpdateTestItemAsync(UpdateTestItemDto dto, CancellationToken ct);
    }
}
