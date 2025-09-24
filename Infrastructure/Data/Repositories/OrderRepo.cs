using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Drawing.Printing;
using DocumentFormat.OpenXml.Vml.Office;
using System.Collections.Concurrent;


namespace NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories
{
    public class OrderRepo
    {
        private readonly LabDbContextSec _db;
        private readonly ConcurrentDictionary<long, object> _orderLocks = new ConcurrentDictionary<long, object>();
        public OrderRepo(LabDbContextSec db)
        {
            _db = db;
        }


        public bool AddOrder(OrderDto order)
        {
            if (order == null) return false;
            var rows = order.rows;
            var labTestInfo = _db.LabTestInfos.FirstOrDefault(i => i.ReportNumber == order.rows[0].reportNum);
            if (labTestInfo != null) return false; // ReportNumber already exists


            var snowflake = new SnowflakeIdGenerator();
            foreach (var row in rows)
            {
                long snowId = snowflake.NextId();
                var csName = _db.CustomerServices.FirstOrDefault(i => i.Id == row.cs)!.CustomerService1;
                var orderEntity = new LabTestInfo
                {
                    Id = snowId,
                    ReportNumber = row.reportNum,
                    OrderEntryPerson = row.orderEntry,
                    Status = 1,
                    Express = row.express,
                    CustomerService = csName,
                    TestGroup = row.group,
                    Remark = order.remark,
                    ScheduleIndex = snowId,
                    LastUpdateTime = DateTime.Now,
                };

                var orderschedule = new LabTestSchedule
                {
                    IdSchedule = snowId,
                    ReportDueDate = row.dueDate,
                    OrderInTime = row.labIn,
                };
                _db.LabTestInfos.Add(orderEntity);
                _db.LabTestSchedules.Add(orderschedule);
            }
            _db.SaveChanges();
            return true;
        }

        public bool UpdateOrder(OrderUpdate order)
        {
            // 参数校验
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }
            // 使用lock语句确保线程安全
            // 获取或创建针对当前订单ID的锁对象
            var orderLock = _orderLocks.GetOrAdd(order.id, _ => new object());
            lock (orderLock)
            {
                try
                {
                    //这里添加实际的订单更新逻辑
                    //例如：
                    var existingOrderInfo = _db.LabTestInfos.FirstOrDefault(o=>o.Id == order.id);
                    var existingOrderSchedule = _db.LabTestSchedules.FirstOrDefault(o => o.Id == order.id);
                    if (existingOrderInfo == null|| existingOrderSchedule == null)
                    {
                        return false;
                    }
                    //
                    // existingOrder.Update(order);
                    // _orderRepository.SaveChanges();

                    return true;
                }
                catch
                {
                    // 记录异常日志
                    // throw; // 根据业务需求决定是否重新抛出异常
                    return false;
                }
            }
        }

        public async Task<OrderOutput[]> GetOrderListAsync(string userId)
        {
            // 1. 先拿昵称（防御空引用）
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return Array.Empty<OrderOutput>();

            // 2. 异步查询并投射
            var flat = await (
                from o in _db.LabTestInfos
                join s in _db.LabTestSchedules on o.ScheduleIndex equals s.IdSchedule
                where o.OrderEntryPerson == user.NickName
                select new
                {
                    o.Id,
                    o.ReportNumber,
                    o.OrderEntryPerson,
                    o.CustomerService,
                    s.OrderInTime,
                    o.Express,
                    o.TestGroup,
                    o.Remark,
                    s.ReportDueDate,
                    Status = o.Status == 1 ? "In Lab"
                                         : o.Status == 2 ? "Review Finished"
                                         : "Completed"
                })
                .ToListAsync();

            // 2. 分组投射
            var orders = flat
                .GroupBy(x => new { x.ReportNumber, x.OrderEntryPerson, x.CustomerService, x.OrderInTime })
                .Select(g => new OrderOutput
                {
                    reportNum = g.Key.ReportNumber,
                    orderEntry = g.Key.OrderEntryPerson,
                    cs = g.Key.CustomerService,
                    labIn = g.Key.OrderInTime,
                    testGroup = g.Select(x => new GroupOutput
                    {
                        recodeId = x.Id,
                        express = x.Express,
                        group = x.TestGroup,
                        remark = x.Remark,
                        dueDate = x.ReportDueDate,
                        status = x.Status
                    }).ToList()
                })
                .ToArray();

            return orders;
        }

        public async Task<PageResult<OrderSummary>> GetCurrentMonthOrdersAsync(int pageNum, int pageSize,int Month)
        {
            var query = from i in _db.LabTestInfos
                        join s in _db.LabTestSchedules
                        on i.Id equals s.IdSchedule
                        where s.ReportDueDate.Month == Month &&
                              s.ReportDueDate.Year == DateTime.Now.Year
                        select new OrderSummary
                        {
                            ReportNum = i.ReportNumber,
                            DueDate = s.ReportDueDate,
                            Cs = i.CustomerService,
                            Testgroup = i.TestGroup,
                            ReviewFinish = s.ReviewFinishTime,
                            OrderEntry = i.OrderEntryPerson,
                            LabIn = s.OrderInTime,
                            LabOut = s.LabOutTime,
                            Remark = i.Remark,
                            Status = i.Status == 1 ? "In Lab"
                                    : i.Status == 2 ? "Review Finished"
                                    : "Completed",
                            Express = i.Express,
                            TestItemNum = i.TestItemNum ?? 0,
                            TestSampleNum = i.TestSampleNum ?? 0

                        };

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(o => o.DueDate)
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PageResult<OrderSummary>
            {
                Items = items,
                TotalCount = total,
                Page = pageNum,
                PageSize = pageSize
            };
        }



        private string? GetExpressName(DateOnly duedate, DateTime labindate)
        {
            string express = "-";
            var days = (duedate.ToDateTime(new TimeOnly()) - labindate).TotalDays + 1;
            if (days <= 2 && days > 0) express = "Same Day";
            else if (days > 2 && days <= 3) express = "Shuttle";
            else if (days > 3 && days <= 4) express = "Express";
            else if (days > 4) express = "Regular";
            return express;
        }
    }
}
