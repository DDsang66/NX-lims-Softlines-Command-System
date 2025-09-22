using NX_lims_Softlines_Command_System.Application.Services.BuyerService;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories;
using NX_lims_Softlines_Command_System.Infrastructure.Services;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;

namespace NX_lims_Softlines_Command_System.Application.Services.Factory
{
    public interface IBuyerFactory
    {
        IBuyer CreateBuyer(string? buyerType);
    }
    public class BuyerFactory : IBuyerFactory
    {
        private readonly LabDbContextSec _dbContext;
        private readonly FiberContentHelper _fiberHelper;
        public BuyerFactory(LabDbContextSec dbContext, FiberContentHelper fiberHelper)
        {
            _dbContext = dbContext;
            _fiberHelper = fiberHelper;
        }

        public IBuyer CreateBuyer(string? buyerType)
        {
            switch (buyerType)
            {
                case "Mango":
                    return new MangoBuyer(new MangoService(new MangoRepository(_dbContext, _fiberHelper), _fiberHelper));
                case "CrazyLine":
                    return new CrazyLineBuyer(new CrazyLineService(new CrazyLineRepository(_dbContext, _fiberHelper), _fiberHelper));
                case "Jako":
                    return new JakoBuyer(new JakoService(new JakoRepository(_dbContext, _fiberHelper), _fiberHelper));
                default:
                    throw new ArgumentException("Invalid buyer type");
            }
        }
    }
}
