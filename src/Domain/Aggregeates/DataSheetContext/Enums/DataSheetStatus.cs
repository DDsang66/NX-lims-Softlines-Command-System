namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.Enums
{
    public enum DataSheetStatus
    {
        /// <summary>
        /// 未知状态
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 生成成功
        /// </summary>
        Created = 1,

        /// <summary>
        /// 进行中
        /// </summary>
        InProccess = 2,

        /// <summary>
        /// 已拒绝
        /// </summary>
        Rejected = 3,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 4,

        /// <summary>
        /// 已发布
        /// </summary>
        Released = 5
    }
}
