using Microsoft.Identity.Client;
using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using System.Threading.Tasks.Dataflow;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext
{
    public sealed class DataSheet:AggregateRoot<DataSheetId,Guid>
    {
        /// <summary>
        /// 当前datasheetUrl
        /// </summary>
        public string Url { get; private set; } = string.Empty;

        /// <summary>
        /// 引用模板的url
        /// </summary>
        public string ContactTemplateUrl { get; private set; } = string.Empty;

        /// <summary>
        /// 当前datasheet状态
        /// </summary>
        public DataSheetStatus Status { get; private set; } = DataSheetStatus.Unknown;

        /// <summary>
        /// 当前datasheet创建时间
        /// </summary>
        public DateTime CreateTime { get; private set; }

        /// <summary>
        /// 当前datasheet更新时间
        /// </summary>
        public DateTime? UpdateTime { get; private set; }

        /// <summary>
        /// ReportNumber
        /// </summary>
        public string ReportNumber { get; private set; } = string.Empty;

        // ★ 领域概念：同一 checklistItem 下的第几个文件
        public int ModelIndex { get; private set; }

        // ★ 领域概念：对应 model 的业务唯一键（如 "Seam-A"），来自 model 本身
        public string? ModelKey { get; private set; }

        /// <summary>
        /// 失败原因（仅 Status = Failed 时有值）
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// 已重试次数
        /// </summary>
        public int RetryCount { get; private set; }

        // ★ 领域概念：生成该文件所用的 model 快照（JSON），
        //   用于避免重复调用 modelGenerator；不参与展示
        public string? ModelSnapshot { get; private set; }

        /// <summary>
        /// 测试项目
        /// </summary>
        public TestItemId TestItemId { get; private set; }

        /// <summary>
        /// datasheet批次
        /// </summary>
        public DataSheetBatchId BatchId { get; private set; }

        /// <summary>
        /// checklistId
        /// </summary>
        public CheckListId CheckListId { get; private set; }

        /// <summary>
        /// datasheet版本号
        /// </summary>
        public string Version { get; private set; } = string.Empty;

        /// <summary>
        /// 创建datasheet聚合根
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="url"></param>
        /// <param name="contactTemplateUrl"></param>
        /// <param name="status"></param>
        /// <param name="reportNumber"></param>
        /// <param name="testItemId"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public static DataSheet Create(
            CheckListId checkListId,
            string? url,
            string contactTemplateUrl,
            DataSheetStatus status,
            string reportNumber,
            TestItemId testItemId,
            DataSheetBatchId batchId,
            int modelIndex)
        {
            if (modelIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(modelIndex));

            var id = new DataSheetId(Guid.NewGuid());
            var dataSheet = new DataSheet
            {
                Id = id,
                CheckListId = checkListId,
                ContactTemplateUrl = contactTemplateUrl,
                Status = status,
                CreateTime = DateTime.Now,
                ReportNumber = reportNumber,
                TestItemId = testItemId,
                ModelIndex = modelIndex,
                ModelKey = null,
                ModelSnapshot = null,
                BatchId = batchId,
                Version = "v1.0"
            };

            if (!string.IsNullOrEmpty(url))
            {
                dataSheet.Url = url;
            }

            return dataSheet;
        }

        /// <summary>
        /// 从持久化层重建聚合根（不触发业务规则，仅用于 ORM 回填）
        /// </summary>
        public static DataSheet Rebuild(
            DataSheetId id,
            CheckListId checkListId,
            DataSheetBatchId batchId,
            TestItemId testItemId,
            string reportNumber,
            string url,
            string contactTemplateUrl,
            DataSheetStatus status,
            int modelIndex,
            string? modelKey,
            string? modelSnapshot,
            string? errorMessage,
            int retryCount,
            string version,
            DateTime createTime,
            DateTime? updateTime)
        {
            return new DataSheet
            {
                Id = id,
                CheckListId = checkListId,
                BatchId = batchId,
                TestItemId = testItemId,
                ReportNumber = reportNumber,
                Url = url ?? string.Empty,
                ContactTemplateUrl = contactTemplateUrl,
                Status = status,
                ModelIndex = modelIndex,
                ModelKey = modelKey,
                ModelSnapshot = modelSnapshot,
                ErrorMessage = errorMessage,
                RetryCount = retryCount,
                Version = version,
                CreateTime = createTime,
                UpdateTime = updateTime
            };
        }

        /// <summary>
        /// 任务被展开时，把对应 model 的业务标识与快照绑定到本聚合根。
        /// 只允许在 Pending 状态下绑定一次，避免重复覆盖。
        /// </summary>
        public void BindModel(string modelKey, string modelSnapshotJson)
        {
            if (Status != DataSheetStatus.Pending && Status != DataSheetStatus.Generating)
                throw new InvalidOperationException($"Cannot bind model when status is {Status}");

            if (string.IsNullOrWhiteSpace(modelKey))
                throw new ArgumentException("modelKey is required", nameof(modelKey));

            ModelKey = modelKey;
            ModelSnapshot = modelSnapshotJson;
            UpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 标记datasheet为生成中
        /// </summary>
        public void MarkGenerating()
        {
            Status = DataSheetStatus.Generating;

            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 标记datasheet为生成成功
        /// </summary>
        /// <param name="fileUrl"></param>
        public void MarkSuccess(string fileUrl)
        {
            Status = DataSheetStatus.Created;
            Url = fileUrl;
            ErrorMessage = null;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 标记datasheet为生成失败
        /// </summary>
        /// <param name="error"></param>
        /// <param name="retryable"></param>
        public void MarkFailed(string error, bool retryable)
        {
            Status = DataSheetStatus.Failed;
            ErrorMessage = error;
            if (retryable) RetryCount++;
            UpdateTime = DateTime.UtcNow;
        }
    }
}
