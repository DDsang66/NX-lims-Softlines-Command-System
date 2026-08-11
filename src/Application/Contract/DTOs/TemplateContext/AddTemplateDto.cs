namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext
{
    public record AddTemplateDto
    {
        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// 节点
        /// </summary>
        public string Site { get; set; } = string.Empty;

        /// <summary>
        /// 模板文件类型
        /// </summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// 模板类型
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 测试类型
        /// </summary>
        public string TestType { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        /// 模板文件
        /// </summary>
        public IFormFile? TemplateFile { get; set; }
    }
}
