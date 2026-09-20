using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class Template
{
    public string Id { get; set; } = null!;

    public string TemplateName { get; set; } = null!;

    public string TemplateUrl { get; set; } = null!;

    public int Site { get; set; }

    public int Status { get; set; }

    public int FileType { get; set; }

    public string BusinessCategory { get; set; } = null!;

    public int Version { get; set; }

    public DateTime UpdateAt { get; set; }

    public string? TemplateIndex { get; set; }

    public virtual ICollection<TemplateStructure> TemplateStructures { get; set; } = new List<TemplateStructure>();

    public virtual ICollection<TestConditionTextTemplate> TestConditionTextTemplates { get; set; } = new List<TestConditionTextTemplate>();
}
