namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Util
{
    public sealed class DocxMergeSection
    {
        public required string FilePath { get; init; }
        /// <summary>仅用于日志/诊断，可为空</summary>
        public string? DisplayName { get; init; }
    }
}
