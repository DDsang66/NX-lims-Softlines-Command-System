namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext.Enums
{
    public enum DataSheetBatchStatus
    {
        /// <summary>
        /// Pending
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Generating
        /// </summary>
        Generating = 1,

        /// <summary>
        /// Completed
        /// </summary>
        Completed = 2,

        /// <summary>
        /// PartialCompleted
        /// </summary>
        PartialFailed = 3,

        /// <summary>
        /// Failed
        /// </summary>
        Failed = 4,

        /// <summary>
        /// Merging
        /// </summary>
        Merging = 5,

        /// <summary>
        /// Merged
        /// </summary>
        Merged =6
    }
}
