using PettyCash.Domain.Common;
using PettyCash.Domain.Exceptions;
using PettyCash.Domain.Settlements.Events;

namespace PettyCash.Domain.Settlements;

/// <summary>
/// Aggregate root for a petty cash settlement. Owns the header, all lines, and the
/// state machine described in ARCHITECTURE.md آ§6. All mutation goes through this
/// class â€” SettlementLine has no public constructor or mutators of its own.
///
/// This class deliberately knows nothing about SharePoint, Postgres, HTTP, or
/// Power Automate. See ARCHITECTURE.md آ§1 â€” if this file ever needs a `using`
/// for any of those, the logic belongs in Application or Infrastructure instead.
/// </summary>
public sealed class Settlement : AggregateRoot<Guid>
{
    private readonly List<SettlementLine> _lines = new();

    public DateOnly SettlementDate { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public string SpenderId { get; private set; } = string.Empty;
    public string SpenderNameSnapshot { get; private set; } = string.Empty;
    public string WorkerIdSnapshot { get; private set; } = string.Empty;
    public string ApproverEmailSnapshot { get; private set; } = string.Empty;
    public SettlementStatus Status { get; private set; }
    public string? ApprovalComment { get; private set; }
    public string? JournalBatchNumber { get; private set; }

    public IReadOnlyList<SettlementLine> Lines => _lines.AsReadOnly();

    /// <summary>Sum of all line gross amounts. Assumes uniform currency across lines (A-005).</summary>
    public decimal TotalAmount => _lines.Sum(l => l.GrossAmount.Amount);

    /// <summary>
    /// True only in Draft. Exposed so callers (e.g. an API controller deciding whether
    /// to render an "Edit" action) don't have to attempt a mutation and catch
    /// InvalidSettlementStateException just to answer a yes/no question.
    /// </summary>
    public bool IsEditable => Status == SettlementStatus.Draft;

    // Parameterless constructor kept private for potential Infrastructure-side object
    // mapping (e.g. EF Core materialization via reflection). Infrastructure still never
    // gets a `using PettyCash.Domain` dependency violation from this â€” it's the other
    // way around â€” but it does mean a repository can rehydrate a Settlement without
    // going through CreateDraft. Rehydration always restores an already-valid state,
    // so bypassing the factory's validation here is safe.
    private Settlement()
    {
    }

    public static Settlement CreateDraft(
        string spenderId,
        string spenderNameSnapshot,
        string workerIdSnapshot,
        string approverEmailSnapshot,
        DateOnly settlementDate,
        string purpose)
    {
        if (string.IsNullOrWhiteSpace(spenderId))
        {
            throw new DomainValidationException("SpenderId is required.");
        }

        if (string.IsNullOrWhiteSpace(approverEmailSnapshot))
        {
            throw new DomainValidationException("An approver is required to create a settlement.");
        }

        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new DomainValidationException("Purpose is required.");
        }

        var settlement = new Settlement
        {
            Id = Guid.NewGuid(),
            SpenderId = spenderId,
            SpenderNameSnapshot = spenderNameSnapshot,
            WorkerIdSnapshot = workerIdSnapshot,
            ApproverEmailSnapshot = approverEmailSnapshot,
            SettlementDate = settlementDate,
            Purpose = purpose,
            Status = SettlementStatus.Draft,
        };

        settlement.Raise(new SettlementCreatedEvent(settlement.Id, DateTime.UtcNow));
        return settlement;
    }

    /// <summary>
    /// Updates the settlement date and purpose while the settlement is still editable (Draft).
    /// Only the two header fields the spender controls are mutable here -- identity snapshots
    /// (SpenderName, WorkerId, ApproverEmail) are fixed at creation time and cannot be
    /// changed after the fact (Guide SS5.2 -- IT maintains profiles, not the spender form).
    /// Frozen-layer exception D-044: additive Domain method required because SettlementDate
    /// and Purpose have private setters. No state machine change; no existing signature changed.
    /// </summary>
    public void UpdateHeader(DateOnly settlementDate, string purpose)
    {
        EnsureStatus(SettlementStatus.Draft, nameof(UpdateHeader));

        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new DomainValidationException("Purpose is required.");
        }

        SettlementDate = settlementDate;
        Purpose = purpose;
    }

    public SettlementLine AddLine(
        string categoryCode,
        string expenseMainAccountSnapshot,
        string? dimensionDefaultsSnapshot,
        decimal grossAmount,
        bool isVat,
        decimal vatRatePercent,
        bool kmRequired,
        OdometerReading? odometer,
        string? notes)
    {
        EnsureStatus(SettlementStatus.Draft, nameof(AddLine));

        var line = new SettlementLine(
            lineNo: _lines.Count + 1,
            categoryCode,
            expenseMainAccountSnapshot,
            dimensionDefaultsSnapshot,
            new Money(grossAmount),
            isVat,
            vatRatePercent,
            kmRequired,
            odometer,
            notes);

        _lines.Add(line);
        return line;
    }

    public void UpdateLine(
        Guid lineId,
        string categoryCode,
        string expenseMainAccountSnapshot,
        string? dimensionDefaultsSnapshot,
        decimal grossAmount,
        bool isVat,
        decimal vatRatePercent,
        bool kmRequired,
        OdometerReading? odometer,
        string? notes)
    {
        EnsureStatus(SettlementStatus.Draft, nameof(UpdateLine));
        var line = FindLineOrThrow(lineId);

        line.Update(
            categoryCode,
            expenseMainAccountSnapshot,
            dimensionDefaultsSnapshot,
            new Money(grossAmount),
            isVat,
            vatRatePercent,
            kmRequired,
            odometer,
            notes);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureStatus(SettlementStatus.Draft, nameof(RemoveLine));
        var line = FindLineOrThrow(lineId);
        _lines.Remove(line);
        RenumberLines();
    }

    public void Submit()
    {
        EnsureStatus(SettlementStatus.Draft, nameof(Submit));

        if (_lines.Count == 0)
        {
            throw new DomainValidationException("Cannot submit a settlement with no lines.");
        }

        Status = SettlementStatus.Submitted;
        Raise(new SettlementSubmittedEvent(Id, TotalAmount, DateTime.UtcNow));
    }

    public void Approve()
    {
        EnsureStatus(SettlementStatus.Submitted, nameof(Approve));
        Status = SettlementStatus.Approved;
        Raise(new SettlementApprovedEvent(Id, DateTime.UtcNow));
    }

    /// <summary>
    /// Submitted â†’ Rejected. Does NOT auto-revert to Draft â€” see DECISIONS.md D-014.
    /// The spender must call <see cref="ReopenForEdit"/> explicitly.
    /// </summary>
    public void Reject(string comment)
    {
        EnsureStatus(SettlementStatus.Submitted, nameof(Reject));

        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainValidationException("A rejection comment is required.");
        }

        Status = SettlementStatus.Rejected;
        ApprovalComment = comment;
        Raise(new SettlementRejectedEvent(Id, comment, DateTime.UtcNow));
    }

    /// <summary>Rejected â†’ Draft. Increments Version (A-002/A-003) so a new approval cycle is distinguishable.</summary>
    public void ReopenForEdit()
    {
        EnsureStatus(SettlementStatus.Rejected, nameof(ReopenForEdit));
        Status = SettlementStatus.Draft;
        Version++;
        Raise(new SettlementReopenedEvent(Id, Version, DateTime.UtcNow));
    }

    public void RecordJournal(string journalBatchNumber)
    {
        EnsureStatus(SettlementStatus.Approved, nameof(RecordJournal));

        if (string.IsNullOrWhiteSpace(journalBatchNumber))
        {
            throw new DomainValidationException("Journal batch number is required.");
        }

        JournalBatchNumber = journalBatchNumber;
        Status = SettlementStatus.Journalled;
        Raise(new SettlementJournalledEvent(Id, journalBatchNumber, DateTime.UtcNow));
    }

    private void EnsureStatus(SettlementStatus required, string action)
    {
        if (Status != required)
        {
            throw new InvalidSettlementStateException(action, Status.ToString());
        }
    }

    private SettlementLine FindLineOrThrow(Guid lineId)
    {
        return _lines.FirstOrDefault(l => l.LineId == lineId)
            ?? throw new DomainValidationException($"Line '{lineId}' was not found on this settlement.");
    }

    private void RenumberLines()
    {
        for (var i = 0; i < _lines.Count; i++)
        {
            _lines[i].LineNo = i + 1;
        }
    }
}
