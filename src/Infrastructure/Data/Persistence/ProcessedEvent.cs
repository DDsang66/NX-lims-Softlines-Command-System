using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class ProcessedEvent
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public DateTime ProcessedAt { get; set; }
}
