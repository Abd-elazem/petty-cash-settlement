using Asp.Versioning;
using Asp.Versioning.Builder;
using FluentValidation;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;
using PettyCash.Application.Exceptions;
using PettyCash.Application.Settlements.Commands;

namespace PettyCash.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for Settlement use cases (Vertical Slice 1: creation; Vertical
/// Slice 2: add line; Vertical Slice 3: submit).
/// Minimal APIs, not MVC controllers, chosen to match Program.cs's existing
/// minimal-hosting style — no Microsoft.AspNetCore.Mvc.Core / AddControllers() service
/// registration exists anywhere in this project, and adding one for a single endpoint
/// would be new infrastructure this slice doesn't need. Revisit only if endpoint
/// count/complexity later makes controller-level conventions (model-binding attributes,
/// filters) earn their keep — not decided here, just not needed yet (D-039).
///
/// The version set is built internally so the call site in Program.cs stays a single
/// `app.MapSettlementsEndpoints();` line — the /api/v{version:apiVersion} convention
/// already established by D-030/ARCHITECTURE.md §9 lives in one place per endpoint group.
/// </summary>
public static class SettlementsEndpoints
{
    public static IEndpointRouteBuilder MapSettlementsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ApiVersionSet versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        RouteGroupBuilder group = endpoints
            .MapGroup("/api/v{version:apiVersion}/settlements")
            .WithApiVersionSet(versionSet)
            .WithTags("Settlements");

        group.MapPost("/", CreateDraftSettlementAsync)
            .WithName("CreateDraftSettlement")
            .WithSummary("Creates a new Draft settlement for the calling spender.")
            .Produces<SettlementDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{settlementId:guid}/lines", AddLineAsync)
            .WithName("AddSettlementLine")
            .WithSummary("Adds a line to an existing Draft (or reopened) settlement.")
            .Produces<SettlementDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{settlementId:guid}/submit", SubmitSettlementAsync)
            .WithName("SubmitSettlement")
            .WithSummary("Submits a Draft (or reopened) settlement for approval.")
            .Produces<SettlementDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateDraftSettlementAsync(
        CreateSettlementRequest request,
        ICommandHandler<CreateDraftSettlementCommand, SettlementDto> handler,
        IValidator<CreateDraftSettlementCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new CreateDraftSettlementCommand(request.SettlementDate, request.Purpose);

        // Application registers validators (AddValidatorsFromAssemblyContaining) but no
        // handler invokes them itself — Application.Tests/Validators/CommandValidatorTests
        // exercises them directly, confirming validation is the *caller's* responsibility
        // by design, not an oversight (D-040). The Api layer is that caller.
        // GlobalExceptionHandler already maps Application.Exceptions.ValidationException to
        // 400 with an `errors` extension, so this reuses that instead of introducing a
        // second validation-error shape.
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new PettyCash.Application.Exceptions.ValidationException(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        SettlementDto dto = await handler.HandleAsync(command, cancellationToken);

        // No GET /settlements/{id} endpoint exists yet (out of scope for this slice,
        // ARCHITECTURE.md §9 already documents the route). The Location path is written
        // to match that documented route so it becomes correct automatically once that
        // endpoint ships, instead of needing a follow-up fix here.
        return Results.Created($"/api/v1/settlements/{dto.RequestId}", dto);
    }

    private static async Task<IResult> AddLineAsync(
        Guid settlementId,
        AddSettlementLineRequest request,
        ICommandHandler<AddLineCommand, SettlementDto> handler,
        IValidator<AddLineCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new AddLineCommand(
            settlementId,
            request.CategoryCode,
            request.GrossAmount,
            request.IsVat,
            request.Notes,
            request.CarPlate,
            request.OdometerKm);

        // Same explicit-validation pattern as CreateDraftSettlementAsync (D-040) — Application
        // registers AddLineCommandValidator but never invokes it itself; the endpoint is the
        // caller responsible for running it before handing off to the handler.
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new PettyCash.Application.Exceptions.ValidationException(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        SettlementDto dto = await handler.HandleAsync(command, cancellationToken);

        // 200, not 201 — AddLine mutates an existing Settlement aggregate; a SettlementLine
        // is not independently addressable (D-003), so there is no new resource URI to report
        // via Location. The updated parent Settlement is returned in the body instead.
        return Results.Ok(dto);
    }

    private static async Task<IResult> SubmitSettlementAsync(
        Guid settlementId,
        ICommandHandler<SubmitSettlementCommand, SettlementDto> handler,
        IValidator<SubmitSettlementCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new SubmitSettlementCommand(settlementId);

        // Same explicit-validation pattern as the other two endpoints (D-040).
        // SubmitSettlementCommandValidator only checks SettlementId != empty (route
        // binding already guarantees a well-formed Guid), so this rarely fails — the
        // real business rules ("must be Draft", "must have at least one line") live in
        // Settlement.Submit() itself and throw Domain-namespace exceptions, mapped to 400
        // by GlobalExceptionHandler's namespace-string match (D-031), same as AddLine's
        // fuel/odometer rule.
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new PettyCash.Application.Exceptions.ValidationException(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        SettlementDto dto = await handler.HandleAsync(command, cancellationToken);

        // 200, not 201 — Submit transitions an existing Settlement's status; no new
        // resource is created (same reasoning as AddLineAsync above, D-003/D-042 precedent).
        return Results.Ok(dto);
    }
}
