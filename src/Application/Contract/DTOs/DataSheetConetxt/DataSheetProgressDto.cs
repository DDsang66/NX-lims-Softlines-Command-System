namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt
{
    public record DataSheetProgressDto
    {
        /// <summary>
        /// CheckListId
        /// </summary>
        public Guid CheckListId { get; set; }

        /// <summary>
        /// BatchId
        /// </summary>
        public Guid BatchId { get; set; }

        /// <summary>
        /// Total number of items
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 成功个数
        /// </summary>
        public int Success { get; set; }

        /// <summary>
        /// 失败个数
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// 正在生成个数
        /// </summary>
        public int Generating { get; set; }

        /// <summary>
        /// 等待个数
        /// </summary>
        public int Pending { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = "PENDING";

        /// <summary>
        /// 生成pdfurl
        /// </summary>
        public string? MergedPdfUrl { get; set; }
        public List<DataSheetItemDto> Items { get; set; } = new();
    }

    public record DataSheetItemDto
    {
        public string DataSheetId { get; set; } = "";
        public string ProjectId { get; set; } = "";
        public string TestItemId { get; set; } = "";
        public int ModelIndex { get; set; }          // ★ 领域字段
        public string? ModelKey { get; set; }        // ★ 领域字段
        public string Status { get; set; } = "PENDING";
        public string? FileUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
