namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class RequiredInfoDto
    {
        public string? buyer { get; set; }
        public string? reportNumber { get; set; }
        public string? reviewer { get; set; }

        public string? menuName { get; set; }
        public string? remark { get; set; }
        public string? extraItem { get; set; }
        public string? standard { get; set; }
        public List<string>? itemName { get; set; }
        public string? washingProcedure { get; set; }
        public string? dryProcedure { get; set; }
        public string? dcProcedure { get; set; }
        public string? sci { get; set; }
        public string? ironProcedure { get; set; }
        public string? bleachProcedure { get; set; }
        public List<FiberDto>? fiberComposition { get; set; }

        public string? additionalRequire { get; set; }

        public string? sampleDescription { get; set; }
    }
}
