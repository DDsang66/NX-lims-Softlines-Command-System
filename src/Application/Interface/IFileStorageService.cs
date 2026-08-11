using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface IFileStorageService:IScopedDependency
    {
        /// <summary>复制模板文件到输出目录，返回目标文件路径</summary>
        string CopyTemplate(string templateRelativePath, string outputDir, string fileName);

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
