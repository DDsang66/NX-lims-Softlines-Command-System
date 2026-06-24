namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class FiberDatabase
{
    public Guid Id { get; set; }
    public string FiberNameEn { get; set; } = string.Empty;
    public string? FiberNameCn { get; set; }
    public string? Category { get; set; }
    public decimal? MoistureRegainIso { get; set; }
    public decimal? MoistureRegainAatcc { get; set; }
    public decimal? MoistureRegainCan { get; set; }
    public decimal? MoistureRegainKor { get; set; }
    public decimal? MoistureRegainGb { get; set; }
    public decimal? MoistureRegainCns { get; set; }
    public decimal? MoistureRegainJis { get; set; }
    public string? QualitativeDescription { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
