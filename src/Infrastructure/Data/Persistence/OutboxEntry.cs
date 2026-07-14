using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class OutboxEntry
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string EventType { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DateTime OccurredOn { get; set; }

    public string AggregateRootId { get; set; } = null!;

    public bool Published { get; set; }

    public DateTime? PublishedAt { get; set; }

    public int RetryCount { get; set; }

    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool DeadLettered { get; set; }
}
