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
    }
}
