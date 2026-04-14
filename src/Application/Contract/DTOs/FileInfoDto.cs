namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    public record FileInfoDto
    {
        /// <summary>
        /// 文件名（用于前端显示）
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 访问URL（用于点击下载/预览）
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（可选，字节）
        /// </summary>
        public long Size { get; set; } = 0;
    }
}
