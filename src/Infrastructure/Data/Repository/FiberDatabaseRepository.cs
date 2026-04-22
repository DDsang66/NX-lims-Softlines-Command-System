using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class FiberDatabaseRepository :  IFiberDatabaseRepository,IScopedDependency
    {
        private readonly LabDbContextSec _context;

        public FiberDatabaseRepository(LabDbContextSec context)
        {
            _context = context;
        }

        public async Task<List<CompositionNew>> GetAllAsync()
        {
            return null;
        }

        public async Task<CompositionNew?> GetByIdAsync(Guid id)
        {
            return null;
        }

        public async Task<CompositionNew?> GetByNameEnAsync(string nameEn)
        {
            return null;
        }

        public async Task<CompositionNew> AddAsync(CompositionNew fiber)
        {
            return null;
        }

        public async Task<CompositionNew> UpdateAsync(CompositionNew fiber)
        {
            return null;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return true;
        }

        public async Task<List<string>> GetAllNamesAsync()
        {
            return null;
        }
    }
}
