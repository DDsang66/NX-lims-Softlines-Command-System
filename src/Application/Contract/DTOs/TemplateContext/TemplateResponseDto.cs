namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext
{
    public record TemplateResponseDto
    {
        public string TemplateId { get; init; } = string.Empty;
        public string TemplateName { get; init; } = string.Empty;
        public string TemplateUrl { get; init; } = string.Empty;
        public string Site { get; init; } = string.Empty;     // 返回枚举的字符串或 int 给前端
        public string Status { get; init; } = string.Empty;
        public string FileType { get; init; } = string.Empty;
        public string BusinessCategory { get; init; } = string.Empty;
        public int Version { get; init; }
        public DateTime UpdateAt { get; init; }
    }
}
