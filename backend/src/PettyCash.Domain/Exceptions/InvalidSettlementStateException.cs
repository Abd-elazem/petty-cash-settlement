namespace PettyCash.Domain.Exceptions;

/// <summary>
/// Thrown when an operation is attempted while the Settlement is not in a status
/// that permits it (e.g. editing a line after Submitted, approving a Draft).
/// See ARCHITECTURE.md §6 for the full state machine.
/// </summary>
public sealed class InvalidSettlementStateException : DomainException
{
    public string AttemptedAction { get; }
    public string CurrentStatus { get; }

    public InvalidSettlementStateException(string attemptedAction, string currentStatus)
        : base($"Cannot perform '{attemptedAction}' while settlement status is '{currentStatus}'.")
    {
        AttemptedAction = attemptedAction;
        CurrentStatus = currentStatus;
    }
}
