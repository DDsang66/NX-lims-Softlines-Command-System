using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.FiberContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class FiberWorksheetRepository : IFiberWorksheetRepository, IScopedDependency
    {
        private readonly LabDbContextSec _context;

        public FiberWorksheetRepository(LabDbContextSec context)
        {
            _context = context;
        }

        public async Task<FiberAnalysis?> GetByReportNumberAsync(string reportNumber)
        {
            return await _context.FiberAnalyses
                .FirstOrDefaultAsync(f => f.ReportNumber == reportNumber);
        }

        public async Task<FiberAnalysis?> GetByIdAsync(long id, CancellationToken ct)
        {
            return await _context.FiberAnalyses.FindAsync(id, ct);
        }

        public async Task AddAsync(FiberAnalysis worksheet, CancellationToken ct)
        {
            worksheet.CreatedAt = DateTime.UtcNow;
            await _context.AddAsync(worksheet, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<FiberAnalysis> UpdateAsync(FiberAnalysis worksheet)
        {
            worksheet.UpdatedAt = DateTime.UtcNow;
            _context.FiberAnalyses.Update(worksheet);
            await _context.SaveChangesAsync();
            return worksheet;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.FiberAnalyses.FindAsync(id);
            if (entity == null) return false;
            _context.FiberAnalyses.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
