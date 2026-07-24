using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TestItemContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface ITestItemAppService:IScopedDependency
    {
        /// <summary>
        /// 创建测试项目
        /// </summary>
        /// <returns></returns>
        Task<Result> AddTestItemAsync();

        /// <summary>
        /// 更新测试项目
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> UpdateTestItemAsync(UpdateTestItemDto dto, CancellationToken ct);

        /// <summary>
        /// 获取测试项目列表
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> GetTestItemByIdAsync(string id, CancellationToken ct);
    }
}
