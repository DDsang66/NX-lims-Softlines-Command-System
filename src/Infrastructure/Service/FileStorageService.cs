using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{
    public class FileStorageService : IFileStorageService, IScopedDependency
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> logger)
        {
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// 获取文件路径
        /// </summary>
        /// <param name="templateRelativePath"></param>
        /// <param name="outputDir"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 保存文件流到指定路径
        /// </summary>
        /// <param name="fileStream"></param>
        /// <param name="targetPath"></param>
        /// <param name="fileUrl"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="IOException"></exception>
        public async Task<string> SaveFileFromStreamAsync(Stream fileStream, string targetPath, string fileUrl)
        {
            // 1. 参数校验
            if (fileStream == null || !fileStream.CanRead)
            {
                throw new ArgumentException("文件流不能为空或不可读", nameof(fileStream));
            }

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                throw new ArgumentException("目标保存地址不能为空", nameof(targetPath));
            }

            // ? 处理完整 URL，提取相对路径
            string relativePath = targetPath;
            if (targetPath.StartsWith("http://") || targetPath.StartsWith("https://"))
            {
                var uri = new Uri(targetPath);
                relativePath = uri.PathAndQuery.TrimStart('/');
            }

            // 移除可能存在的 wwwroot 前缀
            if (relativePath.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Substring("wwwroot/".Length);
            }

            // 组合完整物理路径
            string fullPath = Path.Combine(_env.WebRootPath, relativePath);

            // 校验并处理文件名
            fullPath = ValidateAndNormalizeFileName(fileStream, fullPath);

            // 获取目录路径并确保目录存在
            string directoryPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            try
            {
                using (var fileStreamToWrite = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    if (fileStream.CanSeek)
                    {
                        fileStream.Position = 0;
                    }

                    await fileStream.CopyToAsync(fileStreamToWrite);
                    await fileStreamToWrite.FlushAsync();
                }

                _logger.LogInformation("文件成功保存到 {FullPath}", fullPath);

                // 返回可访问的 URL（相对路径）
                return $"/{relativePath}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文件保存失败，目标路径: {TargetPath}", targetPath);
                throw new IOException($"文件保存失败: {targetPath}", ex);
            }
        }


        /// <summary>
        /// 验证并规范化文件名，确保文件名与目标路径一致
        /// </summary>
        /// <param name="fileStream">文件流</param>
        /// <param name="targetPath">目标路径</param>
        /// <returns>规范化后的目标路径</returns>
        private string ValidateAndNormalizeFileName(Stream fileStream, string targetPath)
        {
            // 从目标路径中提取文件名
            string targetFileName = Path.GetFileName(targetPath);
            string targetDirectory = Path.GetDirectoryName(targetPath);

            // 尝试从文件流中获取原始文件名（如果是 FileStream 或其他支持名称属性的流）
            string sourceFileName = GetFileNameFromStream(fileStream);

            // 如果能够获取到源文件名，且与目标文件名不一致，则记录日志并继续使用目标文件名
            if (!string.IsNullOrEmpty(sourceFileName) && !string.Equals(sourceFileName, targetFileName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "检测到文件名不一致 - 源文件: {SourceFileName}, 目标文件: {TargetFileName}, 将使用目标文件名保存",
                    sourceFileName,
                    targetFileName
                );
            }

            // 如果目标路径中包含文件名，验证并确保目录路径正确
            if (string.IsNullOrEmpty(targetFileName))
            {
                // 如果目标路径没有文件名，尝试从流中获取
                if (!string.IsNullOrEmpty(sourceFileName))
                {
                    targetPath = Path.Combine(targetDirectory ?? string.Empty, sourceFileName);
                    _logger.LogInformation("目标路径缺少文件名，已从流中获取并补充: {TargetPath}", targetPath);
                }
                else
                {
                    throw new ArgumentException("目标路径中缺少文件名，且无法从文件流中获取文件名", nameof(targetPath));
                }
            }

            return targetPath;
        }

        /// <summary>
        /// 从文件流中尝试获取文件名
        /// </summary>
        /// <param name="fileStream">文件流</param>
        /// <returns>文件名，如果无法获取则返回null</returns>
        private string GetFileNameFromStream(Stream fileStream)
        {
            // 尝试获取 FileStream 的 Name 属性
            if (fileStream is FileStream fileStreamObj && !string.IsNullOrEmpty(fileStreamObj.Name))
            {
                return Path.GetFileName(fileStreamObj.Name);
            }

            // 如果是 MemoryStream 或其他类型的流，可以尝试其他方式获取文件名
            // 这里可以根据实际需求扩展，例如从自定义属性或上下文中获取

            return null;
        }
    }
}
