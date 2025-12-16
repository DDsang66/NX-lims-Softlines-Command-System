namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public record ParamDto(
        string ItemName,
        string? Standard,
        string? Temperature,
        string? Program,
        int? SteelBall,
        string? Ballast,
        string? SCI,
        string? DryProcedure,
        string? WashingProcedure,
        string? IsSensitive,
        string? Cycle,
        string? AfterWash,
        string? Param
        );
}
