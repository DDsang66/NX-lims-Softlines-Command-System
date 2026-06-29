using NX_lims_Softlines_Command_System.src.Application.Contract;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{
    public class FileStorageService : IFileStorageService, IScopedDependency
    {
        private readonly IWebHostEnvironment _env;

        public FileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string CopyTemplate(string templateRelativePath, string outputDir, string fileName)
        {
            string sourcePath = Path.Combine(_env.WebRootPath, templateRelativePath);
            string targetDir = Path.Combine(_env.WebRootPath, outputDir);
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);
            string targetPath = Path.Combine(targetDir, fileName);
            File.Copy(sourcePath, targetPath, true);
            return targetPath;
        }
    }
}
