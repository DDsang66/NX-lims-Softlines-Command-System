using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class TestConditionTextTemplate
{
    public Guid Id { get; set; }

    public string TemplateId { get; set; } = null!;

    public string TemplateIndex { get; set; } = null!;

    public string Text { get; set; } = null!;

    public virtual Template Template { get; set; } = null!;
}
