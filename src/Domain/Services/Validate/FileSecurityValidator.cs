using NX_lims_Softlines_Command_System.src.Domain.Contract.Service;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Services.Validate
{
    public class FileSecurityValidator : IFileSecurityValidator,IScopedDependency
    {
        // 允许的扩展名白名单（添加 Word 文件）
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".xlsx", ".xls", ".xlsm",  // Excel 文件
            ".docx", ".doc"            // ✅ 新增：Word 文件
         };

        // 已知安全文件头的 Magic Bytes
        private static readonly Dictionary<string, byte[]> FileSignatures = new()
        {
    // Excel 文件 
            { ".xlsx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } }, // ZIP/XLSX 格式头
            { ".xls", new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } },   // OLE2 格式头
    
    // ✅ 新增：Word 文件           
            { ".docx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } }, // DOCX 也是 ZIP 格式，头与 XLSX 相同 
            { ".doc", new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } }    // DOC 是 OLE2 格式，头与 XLS 相同
        };

        public async Task<FileValidationResult> ValidateAsync(Stream fileStream, string claimedExtension)
        {
            // 1. 验证扩展名白名单
            if (!AllowedExtensions.Contains(claimedExtension))
            {
                return new FileValidationResult(false, $"不允许上传此类型的文件: {claimedExtension}");
            }

            // 2. 验证文件真实类型
            if (!IsValidFileSignature(fileStream, claimedExtension))
            {
                return new FileValidationResult(false, "文件真实类型与扩展名不符，可能是伪装的恶意文件。");
            }

            // 3. 验证文件大小 (防止拒绝服务攻击 DoS)
            if (fileStream.Length > 10 * 1024 * 1024) // 限制 10MB
            {
                return new FileValidationResult(false, "文件大小超过限制 (10MB)。");
            }

            // 4. 杀毒/宏扫描 (如果需要)
            bool isVirusFree = await ScanForMalwareAsync(fileStream);
            if (!isVirusFree)
            {
                return new FileValidationResult(false, "文件包含潜在恶意代码或未通过安全扫描。");
            }

            return new FileValidationResult(true, string.Empty);
        }

        /// <summary>
        /// 验证文件的真实类型
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        private bool IsValidFileSignature(Stream stream, string extension)
        {
            if (!FileSignatures.TryGetValue(extension, out var expectedSignature))
                return true; // 如果没有定义签名规则，暂且放行（或根据业务严格拒绝）

            stream.Position = 0; // 确保在读取头之前重置流的位置
            byte[] buffer = new byte[expectedSignature.Length];

            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead < expectedSignature.Length) return false; // 文件太小，连头都读不出来

            // 对比前 N 个字节
            return buffer.SequenceEqual(expectedSignature);
        }

        /// <summary>
        /// 扫描文件是否包含恶意代码
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private async Task<bool> ScanForMalwareAsync(Stream stream)
        {
            // 这里应集成 ClamAV 等杀毒软件的 SDK 或调用第三方 API
            // 或者使用专业库解析 Excel 检查是否包含危险宏
            // 这里仅作示意
            /* await Task.Delay(10); */// 模拟耗时
            return true;
        }
    }
}
