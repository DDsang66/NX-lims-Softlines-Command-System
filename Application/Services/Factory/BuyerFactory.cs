using NX_lims_Softlines_Command_System.Application.Services.BuyerService;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.BuyerRepos;
using NX_lims_Softlines_Command_System.Infrastructure.Services;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using Microsoft.AspNetCore.Cors.Infrastructure;

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
                case "mango":
                    return new MangoBuyer(new MangoService(new MangoRepository(_dbContext, _fiberHelper), _fiberHelper));
                case "crazyline":
                    return new CrazyLineBuyer(new CrazyLineService(new CrazyLineRepository(_dbContext, _fiberHelper), _fiberHelper));
                case "jako":
                    return new JakoBuyer(new JakoService(new JakoRepository(_dbContext, _fiberHelper), _fiberHelper));
                case "tchibo":
                    return new TchiboBuyer(new TchiboService(new TchiboRepository(_dbContext, _fiberHelper), _fiberHelper));
                case "primark":
                    return new PrimarkBuyer(new PrimarkService(new PrimarkRepository(_dbContext, _fiberHelper), _fiberHelper));
                case "pepco":
                    return new PepcoBuyer(new PepcoService(new PepcoRepository(_dbContext, _fiberHelper), _fiberHelper));
                case "kik":
                    return new KikBuyer(new KikService(new KikRepository(_dbContext, _fiberHelper), _fiberHelper));
                case "next":
                    return new NextBuyer(new NextService(new NextRepository(_dbContext, _fiberHelper), _fiberHelper));
                case "ovs":
                    return new OvsBuyer(new OvsService(new OvsRepository(_dbContext, _fiberHelper), _fiberHelper));
                case "lpp":
                    return new LPPBuyer(new LPPService(new LPPRepository(_dbContext, _fiberHelper), _fiberHelper));
                case "woolworth":
                    return new WoolworthBuyer(new WoolworthService(new WoolworthRepository(_dbContext, _fiberHelper), _fiberHelper));
                default:
                    throw new ArgumentException("Invalid buyer type");
            }
        }
    }
}
