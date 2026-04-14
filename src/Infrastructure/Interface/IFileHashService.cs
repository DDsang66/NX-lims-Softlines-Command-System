using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Interface
{
    public interface IFileHashService:IScopedDependency
    {
        Task<string> ComputeHashAsync(byte[] data);
    }
}
