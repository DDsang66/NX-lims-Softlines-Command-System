namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext
{
    public record UpdateTemplateDto:AddTemplateDto
    {
        /// <summary>
        /// 模板id
        /// </summary>
        public string TemplateId { get; set; } = string.Empty;
    }
}
