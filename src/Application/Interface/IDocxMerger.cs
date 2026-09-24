namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    /// <summary>
    /// 把若干份 docx 的正文按顺序并进第一份，产出**一份**合并稿。
    /// </summary>
    /// <remarks>
    /// 只在纤维模块的多标准出报告里用：N 个标准各走一遍现有渲染管线，最后一步合并。
    /// 合并发生在**渲染之后**，所以模板、ReplaceText、InsertMicroscopeImages 全部零改动
    /// —— 这也是选"先生成 N 份再合并"而不是"改模板加书签"的理由：后者要动模板，
    /// 而且 ReplaceText 按书签名替换，同名书签会被每份都填成同一个值。
    /// **不用 altChunk**：它写法极简，但失败模式是静默的（渲染器不认就整段不显示、
    /// 还不报错），而本模块的预览是 OnlyOffice，查不到它对 altChunk 的支持结论。
    /// 手工合并产出的是纯 OOXML，不依赖任何渲染器特性。
    /// </remarks>
    public interface IDocxMerger
    {
        /// <summary>
        /// 把 <paramref name="sourcePaths"/> 的正文依次并到 <paramref name="basePath"/> 末尾。
        /// </summary>
        /// <param name="basePath">基底文件，**原地修改**；结果就是它。</param>
        /// <param name="sourcePaths">要并入的文件，按给定顺序排列，各自另起一页。</param>
        /// <remarks>
        /// **原地改写 <paramref name="basePath"/>**，不产出新文件 —— 调用方负责把它放在最终位置。
        /// 中间产物与基底**不能是同一个文件**。
        /// </remarks>
        void MergeInto(string basePath, IReadOnlyList<string> sourcePaths);
    }
}
