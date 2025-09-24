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
            if (dto == null || dto.rows == null || dto.rows.Count == 0) return false;
            bool an = _or.AddOrder(dto);
            if (an) return true;
            else return false;
        }


        public async Task<OrderOutput[]> GetOrderListAsync(string userId)
        {
            var result = await _or.GetOrderListAsync(userId);
            return result;
        }


        public async Task<PageResult<OrderSummary>> GetOrderSummaryAsync(int pageNum, int pageSize,int Month)
        {
            if (pageNum <= 0) pageNum = 1;
            if (pageSize <= 0) pageSize = 10;
            if (Month <1||Month>12) Month = DateTime.Now.Month;
            var result = await _or.GetCurrentMonthOrdersAsync(pageNum, pageSize,Month);
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
