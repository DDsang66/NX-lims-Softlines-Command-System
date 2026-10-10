using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Interface
{
    public interface IDocxMergeService: ISingletonDependency
    {
        /// <summary>
        /// 以 baseDocx 为底稿，依次追加若干子文档（每个子文档独立成节）。
        /// </summary>
        /// <param name="baseDocxPath">底稿路径</param>
        /// <param name="sections">要追加的子文档列表（含显示名，可空）</param>
        /// <param name="outputPath">输出路径</param>
        /// <param name="options">可选配置</param>
        Task MergeAsync(
            string baseDocxPath,
            IEnumerable<DocxMergeSection> sections,
            string outputPath,
            DocxMergeOptions? options = null,
            CancellationToken ct = default);
    }
}
