using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class CompositionNew
{
    public int IdComposition { get; set; }

    public string? CompositionNameEn { get; set; }

    public string? CompositionNameChn { get; set; }

    public string? PrimaryCategoryEn { get; set; }

    public string? PrimaryCategoryChn { get; set; }

    public string? SecondaryClassificationEn { get; set; }

    public string? SecondaryClassificationChn { get; set; }

    public string? TertiaryClassificationEn { get; set; }

    public string? TertiaryClassificationChn { get; set; }

    public decimal? MoistureRegainIso { get; set; }

    public decimal? MoistureRegainAatcc { get; set; }

    public decimal? MoistureRegainCan { get; set; }

    public decimal? MoistureRegainKor { get; set; }

    public decimal? MoistureRegainGb { get; set; }

    public decimal? MoistureRegainCns { get; set; }

    public decimal? MoistureRegainJis { get; set; }

    public decimal? QualitativeDescription { get; set; }
}
