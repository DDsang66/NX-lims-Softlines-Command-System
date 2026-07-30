using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TestItemContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.TestItemContext
{
    public interface ITestItemQueryService : IScopedDependency
    {
        /// <summary>
        /// 获取测试项目列表
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<TestItemResponseDto>> GetTestItemByIdAsync(string id, CancellationToken ct);

        /// <summary>
        /// 获取所有测试项目列表
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<List<TestItemResponseDto>>> GetTestItemsAsync(CancellationToken ct);
    }
}
