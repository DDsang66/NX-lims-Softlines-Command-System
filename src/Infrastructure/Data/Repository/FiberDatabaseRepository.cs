using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.FiberContext;
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

        public async Task<List<CompositionNew>> GetAllAsync()
        {
            return await _context.CompositionNews
                .AsNoTracking()
                .Select(c => new CompositionNew
                {
                    IdComposition = c.IdComposition,
                    CompositionNameEn = c.CompositionNameEn,
                    CompositionNameChn = c.CompositionNameChn,
                    PrimaryCategoryEn = c.PrimaryCategoryEn,
                    PrimaryCategoryChn = c.PrimaryCategoryChn,
                    SecondaryClassificationEn = c.SecondaryClassificationEn,
                    SecondaryClassificationChn = c.SecondaryClassificationChn,
                    TertiaryClassificationEn = c.TertiaryClassificationEn,
                    TertiaryClassificationChn = c.TertiaryClassificationChn
                })
                .OrderBy(c => c.CompositionNameEn)
                .ToListAsync();
        }

        public async Task<CompositionNew?> GetByIdAsync(Guid id)
        {
            return null; // CompositionNew 无主键，不可按 ID 查
        }

        public async Task<CompositionNew?> GetByNameEnAsync(string nameEn)
        {
            return await _context.CompositionNews
                .AsNoTracking()
                .Select(c => new CompositionNew
                {
                    IdComposition = c.IdComposition,
                    CompositionNameEn = c.CompositionNameEn
                })
                .FirstOrDefaultAsync(c => c.CompositionNameEn!.ToLower() == nameEn.ToLower());
        }

        public async Task<CompositionNew> AddAsync(CompositionNew fiber)
        {
            _context.CompositionNews.Add(fiber);
            await _context.SaveChangesAsync();
            return fiber;
        }

        public async Task<CompositionNew> UpdateAsync(CompositionNew fiber)
        {
            _context.CompositionNews.Update(fiber);
            await _context.SaveChangesAsync();
            return fiber;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return false; // composition_new 是只读参考表
        }

        public async Task<List<string>> GetAllNamesAsync()
        {
            return await _context.CompositionNews
                .AsNoTracking()
                .OrderBy(c => c.CompositionNameEn)
                .Select(c => c.CompositionNameEn!)
                .Where(n => n != "string")
                .Distinct()
                .ToListAsync();
        }
    }
}
