using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories;

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



        public bool UpdateOrder(OrderUpdateDto dto)
        {
            bool an = _or.UpdateOrder(dto);
            if (an) return true;
            else return false;
        }


    }
}
