using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Contract
{
    public interface ILabelOptionRepository : IScopedDependency
    {
        Task<List<(string Category, string Text)>> GetLabelOptionsAsync(CancellationToken ct);
    }
}
