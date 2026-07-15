namespace PettyCash.Domain.Common;

/// <summary>
/// Marker for domain events raised by aggregates. The Domain layer only raises and
/// records these — it never dispatches or handles them. Application-layer use cases
/// read <see cref="AggregateRoot{TId}.DomainEvents"/> after a successful operation and
/// translate events into audit-log entries, notifications, etc. This keeps AuditLogEntry
/// creation (ARCHITECTURE.md §11) out of the Domain layer entirely.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
