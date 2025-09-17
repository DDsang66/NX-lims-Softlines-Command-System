using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

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
                var orderEntity = new LabTestInfo
                {
                    Id = snowId,
                    ReportNumber = row.reportNum,
                    OrderEntryPerson = row.orderEntry,
                    Status = 1,
                    CustomerService = row.cs,
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

                _db.SaveChanges();
            }
            return true;
        }
    }
}
