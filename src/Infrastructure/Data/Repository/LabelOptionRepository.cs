using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.src.Application.Interface.OrderContext;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class LabelOptionRepository : ILabelOptionRepository
    {
        private readonly LabDbContextSec _context;

        public LabelOptionRepository(LabDbContextSec context)
        {
            _context = context;
        }

        public async Task<List<(string Category, string Text)>> GetLabelOptionsAsync(CancellationToken ct)
            => await _context.LabelOptions
                .OrderBy(o => o.Category).ThenBy(o => o.SortOrder)
                .Select(o => ValueTuple.Create(o.Category, o.Text))
                .ToListAsync(ct);
    }
}
