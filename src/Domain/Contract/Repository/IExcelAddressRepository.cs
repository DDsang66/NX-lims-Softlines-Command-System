namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository
{
    public interface IExcelAddressRepository
    {
        /// <summary>
        /// 根据报告号和类型获取文件路径
        /// </summary>
        Task<string?> GetFilePathAsync(string repoNum, string buyer, string group, CancellationToken ct);
    }
}
