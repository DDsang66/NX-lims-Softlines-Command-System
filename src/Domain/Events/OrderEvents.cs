using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Domain.Events;

public sealed record OrderCreatedEvent : DomainEvent<string>
{
    public OrderCreatedEvent(OrderId orderId) : base(orderId) { }
}

public sealed record OrderLineAddedEvent : DomainEvent<string>
{
    public Guid LineId { get; }
    public OrderLineAddedEvent(OrderId orderId, Guid lineId) : base(orderId)
    {
        LineId = lineId;
    }
}

public sealed record ReviewCompletedEvent : DomainEvent<string>
{
    public Guid LineId { get; }
    public ReviewCompletedEvent(OrderId orderId, Guid lineId) : base(orderId)
    {
        LineId = lineId;
    }
}

public sealed record LabInCompletedEvent : DomainEvent<string>
{
    public Guid LineId { get; }
    public LabInCompletedEvent(OrderId orderId, Guid lineId) : base(orderId)
    {
        LineId = lineId;
    }
}

public sealed record TestDoneEvent : DomainEvent<string>
{
    public Guid LineId { get; }
    public TestDoneEvent(OrderId orderId, Guid lineId) : base(orderId)
    {
        LineId = lineId;
    }
}

public sealed record ReportOutEvent : DomainEvent<string>
{
    public Guid LineId { get; }
    public ReportOutEvent(OrderId orderId, Guid lineId) : base(orderId)
    {
        LineId = lineId;
    }
}

public sealed record OrderLineUpdatedEvent : DomainEvent<string>
{
    public Guid LineId { get; }
    public OrderLineUpdatedEvent(OrderId orderId, Guid lineId) : base(orderId)
    {
        LineId = lineId;
    }
}

public sealed record OrderLineDeletedEvent : DomainEvent<string>
{
    public Guid LineId { get; }
    public OrderLineDeletedEvent(OrderId orderId, Guid lineId) : base(orderId)
    {
        LineId = lineId;
    }
}
