namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    public record DocxUrlResponseDto
    {
        public string fileKey { get; init; } = string.Empty;
        public string fileName { get; init; } = string.Empty;
        public string downloadUrl { get; init; } = string.Empty;
        public string callbackUrl { get; init; } = string.Empty;
    }
}
