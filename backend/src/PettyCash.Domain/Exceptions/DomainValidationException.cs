namespace PettyCash.Domain.Exceptions;

/// <summary>
/// Thrown when a value fails a Domain-level validation rule (e.g. negative amount,
/// missing odometer reading on a fuel line, unsupported currency).
/// </summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message)
    {
    }
}
