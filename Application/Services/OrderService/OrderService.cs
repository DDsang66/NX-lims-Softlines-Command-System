using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Data.Repositories;

namespace NX_lims_Softlines_Command_System.Application.Services.OrderService
{
    public class OrderService
    {
        private readonly OrderRepo _or;
        public OrderService(OrderRepo or)
        {
            _or = or;
        }


        public bool AddOrder(OrderDto dto)
        {
            if (dto == null || dto.Rows == null || dto.Rows.Count == 0) return false;
            bool an = _or.AddOrder(dto);
            if (an) return true;
            else return false;
        }


        public async Task<OrderOutput[]> GetOrderListAsync(string userId)
        {
            var result = await _or.GetOrderListAsync(userId);
            return result;
        }


        public async Task<object> GetOrderSummaryAsync(OrderQueryParams dto)
        {
            var result = await _or.GetSummaryOrdersAsync(dto);
            return result;
        }

        public bool DeleteOrder(OrderDeleteRequest dto)
        {
            if(dto == null || dto.UserId == null) return false;
            bool an = _or.DeleteOrder(dto);
            if (an) return true;
            else return false;
        }

        public bool UpdateOrder(OrderUpdateDto dto)
        {
            bool an = _or.UpdateOrder(dto);
            if (an) return true;
            else return false;
        }


        public async Task<OrderCardOutput> GetOrderCardListAsync(DateTimeOffset time, string group, string timeType)
        {
            DateTimeOffset utcTime = time.ToUniversalTime().ToOffset(TimeSpan.FromHours(8));
            var result = await _or.OrderCardAsync(utcTime, group,timeType);
            return result;
        }

        public async Task<OrderFanCardOutput> GetOrderFanChartListAsync(DateTimeOffset time, string group, string timeType)
        {
            DateTimeOffset utcTime = time.ToUniversalTime().ToOffset(TimeSpan.FromHours(8));
            var result = await _or.OrderfanCardAsync(utcTime, group, timeType);
            return result;
        }

        public async Task<OrderLineCardOutput> GetOrderLineChartAsync(DateTimeOffset[] time, string group, string timeType,string Type)
        {
            var utcTimeArray = time.Select(t => t.ToUniversalTime().ToOffset(TimeSpan.FromHours(8))).ToArray();
            var result = await _or.OrderLineChartAsync(utcTimeArray, group, timeType,Type);
            return result;
        }
    }
}
