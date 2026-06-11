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
                .AsNoTracking()
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

            // 1) 删旧子实体（纯 SQL，不依赖追踪器）
            await _context.FiberWorksheetDetails
                .Where(d => d.WorksheetId == worksheet.Id).ExecuteDeleteAsync();
            await _context.FiberWorksheetResults
                .Where(r => r.WorksheetId == worksheet.Id).ExecuteDeleteAsync();

            // 2) 更新主表（纯 SQL）
            await _context.FiberWorksheets
                .Where(w => w.Id == worksheet.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(w => w.UpdatedAt, worksheet.UpdatedAt)
                    .SetProperty(w => w.ComponentType, worksheet.ComponentType)
                    .SetProperty(w => w.TestMethod, worksheet.TestMethod)
                    .SetProperty(w => w.Buyer, worksheet.Buyer));

            // 3) 插入新子实体（无追踪冲突，因为旧实体从未被追踪）
            if (worksheet.Details.Any())
                _context.FiberWorksheetDetails.AddRange(worksheet.Details);
            if (worksheet.Result != null)
                _context.FiberWorksheetResults.Add(worksheet.Result);

            if (worksheet.Details.Any() || worksheet.Result != null)
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
