using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service
{
    public interface IFileSecurityValidator: IScopedDependency
    {
        /// <summary>
        /// 验证上传文件的安全性
        /// </summary>
        /// <param name="fileStream">文件流</param>
        /// <param name="claimedExtension">前端声称的扩展名 (如 ".xlsx")</param>
        /// <returns>验证结果</returns>
        Task<FileValidationResult> ValidateAsync(Stream fileStream, string claimedExtension);

    }
}
