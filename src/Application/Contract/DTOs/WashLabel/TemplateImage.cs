namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.WashLabel;

public class TemplateImage
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Base64Data { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;

    public string DataUri => $"data:{MediaType};base64,{Base64Data}";
}
