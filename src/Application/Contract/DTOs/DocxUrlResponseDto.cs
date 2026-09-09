namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    public record DocxUrlResponseDto
    {
        /// <summary>
        /// Key of the file
        /// </summary>
        public string fileKey { get; init; } = string.Empty;

        /// <summary>
        /// 文件名称
        /// </summary>
        public string fileName { get; init; } = string.Empty;

        /// <summary>
        /// 下载链接
        /// </summary>
        public string downloadUrl { get; init; } = string.Empty;

        /// <summary>
        /// 回调链接
        /// </summary>
        public string callbackUrl { get; init; } = string.Empty;
    }
}
