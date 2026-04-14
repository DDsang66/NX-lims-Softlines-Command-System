using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
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

        public async Task<FiberWorksheet?> GetByReportNumberAsync(string reportNumber)
        {
            return await _context.FiberWorksheets
                .Include(w => w.Details)
                .Include(w => w.Result)
                .FirstOrDefaultAsync(w => w.ReportNumber == reportNumber);
        }

        public async Task<FiberWorksheet?> GetByIdAsync(Guid id)
        {
            return await _context.FiberWorksheets
                .Include(w => w.Details)
                .Include(w => w.Result)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<FiberWorksheet?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.FiberWorksheets
                .Include(w => w.Details)
                .Include(w => w.Result)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<FiberWorksheet> AddAsync(FiberWorksheet worksheet)
        {
            worksheet.CreatedAt = DateTime.UtcNow;
            _context.FiberWorksheets.Add(worksheet);
            await _context.SaveChangesAsync();
            return worksheet;
        }

        public async Task<FiberWorksheet> UpdateAsync(FiberWorksheet worksheet)
        {
            worksheet.UpdatedAt = DateTime.UtcNow;
            _context.FiberWorksheets.Update(worksheet);
            await _context.SaveChangesAsync();
            return worksheet;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var worksheet = await GetByIdAsync(id);
            if (worksheet == null) return false;

            _context.FiberWorksheets.Remove(worksheet);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
