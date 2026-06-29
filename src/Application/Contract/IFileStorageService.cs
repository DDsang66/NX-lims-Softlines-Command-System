namespace NX_lims_Softlines_Command_System.src.Application.Contract
{
    public interface IFileStorageService
    {
        /// <summary>复制模板文件到输出目录，返回目标文件路径</summary>
        string CopyTemplate(string templateRelativePath, string outputDir, string fileName);
    }
}
