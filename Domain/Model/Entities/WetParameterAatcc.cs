using NX_lims_Softlines_Command_System.Domain.Model.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class WetParameterAatcc:IWetParam
{
    [Key]
    public int ParamId { get; set; }
    public string? ReportNumber { get; set; }

    public string? StandardType { get; set; }

    public string? WashingProcedure { get; set; }

    public string? DryProcedure { get; set; }

    public string? DryCleanProcedure { get; set; }

    public string? SpecialCareInstruction { get; set; }

    public string? Iron { get; set; }

    public string? Bleach { get; set; }

    public string? Cycle { get; set; }

    public string? Program { get; set; }

    public string? DryCondition { get; set; }

    public string? Temperature { get; set; }

    public int? SteelBallNum { get; set; }

    public string? SteelBallType { get; set; }

    public string? Sensitive { get; set; }

    public int? AfterWash { get; set; }

    public string? ContactItem { get; set; }
}
