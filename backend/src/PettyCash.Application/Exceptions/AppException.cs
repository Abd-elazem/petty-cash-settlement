namespace PettyCash.Application.Exceptions;

/// <summary>
/// Base type for exceptions raised by the Application layer (as opposed to
/// PettyCash.Domain.Exceptions.DomainException, raised for business-rule violations
/// inside the aggregate itself). Named AppException, not ApplicationException, to avoid
/// colliding with System.ApplicationException.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} '{key}' was not found.")
    {
    }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}

/// <summary>
/// Wraps FluentValidation failures into an Application-owned type so callers (eventually
/// the Api layer) don't need a FluentValidation reference just to catch this.
/// </summary>
public sealed class ValidationException : AppException
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(IEnumerable<string> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors.ToList();
    }
}

public sealed class ConcurrencyException : AppException
{
    public ConcurrencyException(string entityName, object key)
        : base($"{entityName} '{key}' was modified by another process. Reload and try again.")
    {
    }
}
