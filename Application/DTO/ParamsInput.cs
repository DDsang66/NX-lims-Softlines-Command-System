namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class ParamsInput
    {
        public string? ItemName { get; set; }
        public string? OrderNumber { get; set; }
        public string? WashingProcedure { get; set; }
        public string? DryProcedure { get; set; }
        public string? DCProcedure { get; set; }
        public string? Sci { get; set; }
        public string? Iron { get; set; }
        public string? Bleach { get; set; }
        public List<FiberDto>? FiberContent { get; set; }
        public string? additionalRequire { get; set; }
        public string? SampleDescription { get; set; }
    }



    public class FiberDto
    {
        public string? Composition { get; set; }
        public int Rate { get; set; }
    }
}
