namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Util
{

    /// <summary>
    /// condition不同项
    /// </summary>
    public class ConditionDiff
    {
        /// <summary>
        /// 新增项
        /// </summary>
        public Dictionary<string, object?> Added { get; } = new();

        /// <summary>
        /// 删除项
        /// </summary>
        public Dictionary<string, object?> Removed { get; } = new();

        /// <summary>
        /// 修改项
        /// </summary>
        public Dictionary<string, (object? Left, object? Right)> Modified { get; } = new();

        /// <summary>
        /// 是否有不同
        /// </summary>
        public bool HasDifferences => Added.Any() || Removed.Any() || Modified.Any();
    }
}
