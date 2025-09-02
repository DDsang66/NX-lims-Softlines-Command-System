using System;
using NX_lims_Softlines_Command_System.Application.Interfaces;
using NX_lims_Softlines_Command_System.Application.Services;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories;
using NX_lims_Softlines_Command_System.Infrastructure.Services;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using NX_lims_Softlines_Command_System.Models;

namespace NX_lims_Softlines_Command_System.Application.Services.Factory
{
    public interface IBuyerFactory
    {
        IBuyer CreateBuyer(string? buyerType);
    }
    public class BuyerFactory : IBuyerFactory
    {
        private readonly LabDbContext _dbContext;
        private readonly LabDbContextSec _dbContextSec;
        private readonly FiberContentHelper _fiberHelper;
        public BuyerFactory(LabDbContext dbContext, LabDbContextSec dbContextSec, FiberContentHelper fiberHelper)
        {
            _dbContext = dbContext;
            _dbContextSec = dbContextSec;
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
                    return new JakoBuyer(new JakoService(new JakoRepository(_dbContextSec, _fiberHelper), _fiberHelper));
                default:
                    throw new ArgumentException("Invalid buyer type");
            }
        }
    }
}
