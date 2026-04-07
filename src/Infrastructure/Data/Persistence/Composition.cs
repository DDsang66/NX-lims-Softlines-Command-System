using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class Composition
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
}
