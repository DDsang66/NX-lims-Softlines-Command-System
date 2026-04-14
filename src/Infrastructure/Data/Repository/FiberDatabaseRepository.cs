using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class FiberDatabaseRepository : IFiberDatabaseRepository, IScopedDependency
    {
        private readonly LabDbContextSec _context;

        public FiberDatabaseRepository(LabDbContextSec context)
        {
            _context = context;
        }

        public async Task<List<FiberDatabase>> GetAllAsync()
        {
            return await _context.FiberDatabases
                .Where(f => f.IsActive)
                .OrderBy(f => f.FiberNameEn)
                .ToListAsync();
        }

        public async Task<FiberDatabase?> GetByIdAsync(Guid id)
        {
            return await _context.FiberDatabases.FindAsync(id);
        }

        public async Task<FiberDatabase?> GetByNameEnAsync(string nameEn)
        {
            return await _context.FiberDatabases
                .FirstOrDefaultAsync(f => f.FiberNameEn.ToLower() == nameEn.ToLower());
        }

        public async Task<FiberDatabase> AddAsync(FiberDatabase fiber)
        {
            fiber.CreatedAt = DateTime.UtcNow;
            _context.FiberDatabases.Add(fiber);
            await _context.SaveChangesAsync();
            return fiber;
        }

        public async Task<FiberDatabase> UpdateAsync(FiberDatabase fiber)
        {
            fiber.UpdatedAt = DateTime.UtcNow;
            _context.FiberDatabases.Update(fiber);
            await _context.SaveChangesAsync();
            return fiber;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var fiber = await GetByIdAsync(id);
            if (fiber == null) return false;

            fiber.IsActive = false;
            fiber.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> GetAllNamesAsync()
        {
            return await _context.FiberDatabases
                .Where(f => f.IsActive)
                .OrderBy(f => f.FiberNameEn)
                .Select(f => f.FiberNameEn)
                .ToListAsync();
        }
    }
}
