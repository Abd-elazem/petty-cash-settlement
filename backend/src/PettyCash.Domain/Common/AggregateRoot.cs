namespace PettyCash.Domain.Common;

/// <summary>
/// Base type for aggregate roots. Owns identity, optimistic-concurrency versioning,
/// and the buffer of domain events raised during the current operation.
/// The Domain layer never dispatches these events — Application reads
/// <see cref="DomainEvents"/> after a successful use case and clears them via
/// <see cref="ClearDomainEvents"/>. See IDomainEvent.cs for the full rationale.
/// </summary>
public abstract class AggregateRoot<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Optimistic-concurrency token. Infrastructure adapters map this to whatever
    /// their storage uses natively (Postgres xmin/rowversion, SharePoint __etag).
    /// The Domain layer only increments it; it never interprets the value.
    /// </summary>
    public int Version { get; protected set; } = 1;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
