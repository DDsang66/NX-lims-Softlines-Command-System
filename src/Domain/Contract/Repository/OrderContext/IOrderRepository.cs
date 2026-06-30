using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.OrderContext
{
    /// <summary>
    /// 订单领域仓储接口（定义在 Domain 层，实现在 Infrastructure 层）
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>新增订单聚合根</summary>
        Task AddAsync(Order order, CancellationToken ct);

        /// <summary>更新订单聚合根</summary>
        Task UpdateAsync(Order order, CancellationToken ct);

        /// <summary>根据 ReportNumber 查找订单</summary>
        Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct);

        /// <summary>判断 ReportNumber + TestGroup 是否已存在</summary>
        Task<bool> ExistsAsync(OrderId id, string testGroup, CancellationToken ct);

        /// <summary>根据 LineId 查所属 ReportNumber</summary>
        Task<string?> GetReportNumberByLineIdAsync(long lineId, CancellationToken ct);
    }
}
