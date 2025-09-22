using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model;

namespace NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories
{
    public class OrderRepo
    {
        private readonly LabDbContextSec _db;
        public OrderRepo(LabDbContextSec db)
        {
            _db = db;
        }


        public bool AddOrder(OrderDto order)
        {
            if (order == null) return false;
            var rows = order.rows;
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
                    CustomerService = csName,
                    TestGroup = row.group,
                    Remark = order.remark,
                    ScheduleIndex = snowId
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

        public bool UpdateOrder(OrderDto order)
        {

            return true;
        }

        public async Task<OrderOutput[]> GetOrderListAsync(string userId)
        {
            // 1. 先拿昵称（防御空引用）
            var user = await _db.Users
                                .Where(u => u.UserId == userId)
                                .Select(u => u.NickName)
                                .FirstOrDefaultAsync();
            if (user == null) return Array.Empty<OrderOutput>();

            // 2. 异步查询并投射
            var orders = await (from o in _db.LabTestInfos
                                join s in _db.LabTestSchedules
                                     on o.ScheduleIndex equals s.IdSchedule
                                where o.OrderEntryPerson == user
                                select new OrderOutput
                                {
                                    reportNum = o.ReportNumber,
                                    orderEntry = o.OrderEntryPerson,
                                    express = GetExpressName(s.ReportDueDate, s.OrderInTime),
                                    dueDate = s.ReportDueDate,
                                    cs = o.CustomerService,
                                    testgroup = o.TestGroup,
                                    labIn = s.OrderInTime,
                                    remark = o.Remark,
                                    status = o.Status == 1 ? "In Lab"
                                    : o.Status == 2 ? "Review Finished"
                                    : "Completed"
                                }).ToArrayAsync();   // 异步执行
            return orders;
        }


        private string? GetExpressName(DateOnly duedate, DateTime labindate)
        {
            string express = "-";
            var days = (duedate.ToDateTime(new TimeOnly()) - labindate).TotalDays;
            if (days <= 1) express = "Same Date";
            else if (days > 1 && days <= 2) express = "Shuttle";
            else if (days > 2 && days <= 3) express = "Express";
            else if (days > 3) express = "Regular";
            return express;
        }
    }
}
