namespace PettyCash.Domain.Exceptions;

/// <summary>
/// Base type for all Domain-layer exceptions. Application/Api layers can catch this
/// as a single type to translate into a 4xx response without knowing which specific
/// invariant was violated.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
