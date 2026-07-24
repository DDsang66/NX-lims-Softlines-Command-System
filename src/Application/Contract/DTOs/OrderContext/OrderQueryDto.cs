using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.OrderContext;

/// <summary>
/// 订单查询参数 — 前端 OrderSummary 筛选条件
/// </summary>
public class OrderQueryParams
{
    /// <summary>查询条件键值对（如 Group、Status、时间范围等）</summary>
    public Dictionary<string, object>? QueryParam { get; set; }

    /// <summary>页码（1-based）</summary>
    public int PageNum { get; set; }

    /// <summary>每页条数</summary>
    public int PageSize { get; set; }
}

/// <summary>
/// LabTest 关联查询结果 — Info + Schedule 两表 JOIN
/// </summary>
public sealed class LabTestJoinDto
{
    public required LabTestInfo Info { get; init; }
    public required LabTestInfo Schedule { get; init; }
}
