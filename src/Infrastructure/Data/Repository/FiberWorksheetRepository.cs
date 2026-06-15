using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.FiberContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class FiberWorksheetRepository : IFiberWorksheetRepository,IScopedDependency
    {
        private readonly LabDbContextSec _context;

        public FiberWorksheetRepository(LabDbContextSec context)
        {
            _context = context;
        }

        public async Task<FiberAnalysis?> GetByReportNumberAsync(string reportNumber)
        {
            return null;
        }

        public async Task<FiberAnalysis?> GetByIdAsync(long id, CancellationToken ct)
        {
            var fiberAnalysis = await _context.FiberAnalyses.FindAsync(id);

            return fiberAnalysis;
        }


        public async Task AddAsync(FiberAnalysis worksheet,CancellationToken ct)
        {
            await _context.AddAsync(worksheet,ct);

            await _context.SaveChangesAsync(ct);
        }

        public async Task<FiberAnalysis> UpdateAsync(FiberAnalysis worksheet)
        {

            return null;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return true;
        }
    }
}
