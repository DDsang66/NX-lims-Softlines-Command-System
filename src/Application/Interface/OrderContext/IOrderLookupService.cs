namespace NX_lims_Softlines_Command_System.src.Application.Interface.OrderContext
{
    /// <summary>
    /// 订单辅助查询接口 — 对 DbContext 的简单查询进行接口隔
    /// </summary>
    public interface IOrderLookupService
    {
        Task<string> ResolveCsNameAsync(int? csId);
        Task<string?> ResolveUserNameAsync(string? userId);
    }
}
