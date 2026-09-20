using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext
{
    public class DataSheetBatch: AggregateRoot<DataSheetBatchId, Guid>
    {
        /// <summary>
        /// 外键，对应的checklistId
        /// </summary>
        public CheckListId CheckListId { get; private set; }

        /// <summary>
        /// 报告编号
        /// </summary>
        public string ReportNo { get; private set; }

        /// <summary>
        /// 总数量
        /// </summary>
        public int Total { get; private set; }

        /// <summary>
        /// 批次状态
        /// </summary>
        public DataSheetBatchStatus Status { get; private set; }

        /// <summary>
        /// 模板url
        /// </summary>
        public string TemplateUrl { get; private set; }

        /// <summary>
        /// 合并后的pdf url
        /// </summary>
        public string? MergedPdfUrl { get; private set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; private set; }

        private DataSheetBatch() { }

        /// <summary>
        /// 创建一个新的批次
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="reportNo"></param>
        /// <param name="total"></param>
        /// <param name="templateUrl"></param>
        /// <returns></returns>
        public static DataSheetBatch Create(
            CheckListId checkListId,
            string reportNo,
            int total,
            string templateUrl)
        {
            return new DataSheetBatch
            {
                Id = new DataSheetBatchId(Guid.NewGuid()),
                CheckListId = checkListId,
                ReportNo = reportNo,
                Total = total,
                Status = DataSheetBatchStatus.Pending,
                TemplateUrl = templateUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 从持久化层重建聚合根（不触发业务规则，仅用于 ORM 回填）
        /// </summary>
        public static DataSheetBatch Rebuild(
            DataSheetBatchId id,
            CheckListId checkListId,
            string reportNo,
            int total,
            DataSheetBatchStatus status,
            string templateUrl,
            string? mergedPdfUrl,
            DateTime createdAt,
            DateTime updatedAt)
        {
            return new DataSheetBatch
            {
                Id = id,
                CheckListId = checkListId,
                ReportNo = reportNo,
                Total = total,
                Status = status,
                TemplateUrl = templateUrl,
                MergedPdfUrl = mergedPdfUrl,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };
        }

        /// <summary>
        /// 标记为生成中
        /// </summary>
        public void MarkGenerating()
        {
            Status = DataSheetBatchStatus.Generating;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 标记为完成
        /// </summary>
        /// <param name="success"></param>
        /// <param name="failed"></param>
        public void MarkCompleted(int success, int failed)
        {
            Status = failed == 0 ? DataSheetBatchStatus.Completed
                 : success == 0 ? DataSheetBatchStatus.Failed
                 : DataSheetBatchStatus.PartialFailed;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 标记为合并完成
        /// </summary>
        /// <param name="mergedPdfUrl"></param>
        public void MarkMerged(string mergedPdfUrl)
        {
            MergedPdfUrl = mergedPdfUrl;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
