using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.src.Application.Interface.OrderContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{
    /// <summary>
    /// 订单辅助查询服务 — 封装 DbContext 的简单查
    /// </summary>
    public class OrderLookupService : IOrderLookupService, IScopedDependency
    {
        private readonly LabDbContextSec _db;

        public OrderLookupService(LabDbContextSec db)
        {
            _db = db;
        }

        public async Task<string> ResolveCsNameAsync(int? csId)
        {
            if (csId == null) return string.Empty;
            var cs = await _db.CustomerServices.FirstOrDefaultAsync(i => i.Id == csId);
            return cs?.CustomerService1 ?? string.Empty;
        }

        public async Task<string?> ResolveUserNameAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            return user?.NickName;
        }
    }
}
