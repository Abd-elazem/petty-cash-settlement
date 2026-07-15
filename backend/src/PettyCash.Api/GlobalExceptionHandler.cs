using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PettyCash.Application.Exceptions;

namespace PettyCash.Api;

/// <summary>
/// Maps the two exception hierarchies established in Application (Milestone 0.3) and Domain
/// (Milestone 0.2) onto RFC 9457 ProblemDetails responses. This is the one place that
/// translation happens — no controller/endpoint should ever catch these itself.
///
/// Domain exceptions are matched by namespace, not by importing PettyCash.Domain.Exceptions —
/// Api must reference only Application and Infrastructure, never Domain directly (Sprint 5
/// rule). A compile-time `using`/type reference here would violate that even though it would
/// technically build (Domain types are transitively visible via Application's own reference).
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const string DomainExceptionsNamespace = "PettyCash.Domain.Exceptions";

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Failed"),
            ConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency Conflict"),
            _ when exception.GetType().Namespace == DomainExceptionsNamespace => (StatusCodes.Status400BadRequest, "Business Rule Violation"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            // Only genuinely unexpected exceptions get logged at Error — the mapped ones above
            // are expected, client-facing outcomes (a 404/403/400/409 is not a server fault).
            _logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : exception.Message,
            Instance = httpContext.Request.Path,
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }
}
