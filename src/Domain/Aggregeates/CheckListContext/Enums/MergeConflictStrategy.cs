namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.Enums
{
    public enum MergeConflictStrategy
    {
        Overwrite,    // 覆盖（默认）
        Ignore,       // 忽略冲突，保留当前
        Throw,        // 抛出异常
        CombineList   // 尝试合并为列表
    }
}

