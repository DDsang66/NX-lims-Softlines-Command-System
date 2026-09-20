using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class TemplateStructure
{
    public Guid Id { get; set; }

    public string TemplateId { get; set; } = null!;

    public int TestConditionCount { get; set; }

    public int TestMethodCount { get; set; }

    public int SampleDataAreaCount { get; set; }

    public int SampleResultAreaCount { get; set; }

    public int AfterWashDataCount { get; set; }

    public virtual Template Template { get; set; } = null!;
}
