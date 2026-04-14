using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;
using System.Security.Cryptography;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{
    public class FileHashService : IFileHashService, IScopedDependency
    {
        public async Task<string> ComputeHashAsync(byte[] data)
        {
            using var md5 = MD5.Create();
            var hash = await Task.Run(() => md5.ComputeHash(data)); // 如果文件很大，这里确实涉及线程，但在 Infra 层处理
            return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
        }
    }
}
