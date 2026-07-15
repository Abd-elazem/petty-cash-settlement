using PettyCash.Domain.Common;

namespace PettyCash.Domain.Settlements.Events;

public sealed record SettlementCreatedEvent(Guid SettlementId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SettlementSubmittedEvent(Guid SettlementId, decimal TotalAmount, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SettlementApprovedEvent(Guid SettlementId, DateTime OccurredAtUtc) : IDomainEvent;

/// <summary>Rejected is terminal-until-reopened, not an auto-revert — see DECISIONS.md D-014.</summary>
public sealed record SettlementRejectedEvent(Guid SettlementId, string Comment, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SettlementReopenedEvent(Guid SettlementId, int NewVersion, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SettlementJournalledEvent(Guid SettlementId, string JournalBatchNumber, DateTime OccurredAtUtc) : IDomainEvent;
