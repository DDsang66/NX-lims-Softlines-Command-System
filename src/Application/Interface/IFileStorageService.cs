using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface IFileStorageService:IScopedDependency
    {
        /// <summary>复制模板文件到输出目录，返回目标文件路径</summary>
        string CopyTemplate(string templateRelativePath, string outputDir, string fileName);

        /// <summary>
        /// 复制模板到**绝对路径**（新增）。
        /// </summary>
        /// <param name="templateRelativePath">相对 WebRootPath 的模板路径。</param>
        /// <param name="absoluteTargetPath">目标的绝对路径，父目录不存在时自动创建。</param>
        /// <remarks>
        /// 与 <see cref="CopyTemplate"/> 的区别只在**目标**：那个把源和目标都钉死在 WebRootPath 下，
        /// 而这个允许写到任意绝对路径 —— 多标准报告的中间产物要落 %TEMP%
        /// （**不能落 wwwroot/DocxModel/SaveDocx/**，那是可下载目录，中间产物进去就等于对外可见）。
        /// 加这个方法而不是给 FiberWorksheetService 注入 IWebHostEnvironment：
        /// "路径以 WebRootPath 为基准"这条知识留在存储服务里，调用方不该知道模板存在哪。
        /// </remarks>
        void CopyTemplateTo(string templateRelativePath, string absoluteTargetPath);

        /// <summary>
        /// 接收文件数据流并保存到指定地址
        /// </summary>
        /// <param name="fileStream">文件输入流（Excel或Docx等）</param>
        /// <param name="targetPath">文件保存的绝对路径或相对路径</param>
        /// <param name="fileUrl">文件访问的URL</param>
        /// <returns>保存成功后返回文件的访问URL</returns>
        Task<string> SaveFileFromStreamAsync(Stream fileStream, string targetPath, string fileUrl);
    }
}
