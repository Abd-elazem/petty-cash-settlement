using Asp.Versioning;
using Asp.Versioning.Builder;
using FluentValidation;
using PettyCash.Api.DependencyInjection;
using PettyCash.Application.Common;
using PettyCash.Application.Abstractions;
using PettyCash.Application.DTOs;
using PettyCash.Application.Exceptions;
using PettyCash.Application.Settlements.Commands;
using PettyCash.Application.Settlements.Queries;
using AppValidationException = PettyCash.Application.Exceptions.ValidationException;

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
            .WithTags("Settlements")
            .RequireAuthorization();

        group.MapPost("/", CreateDraftSettlementAsync)
            .WithName("CreateDraftSettlement")
            .WithSummary("Creates a new Draft settlement for the calling spender.")
            .RequireAuthorization(EntraAuthorizationPolicies.ForRole(UserRole.Spender))
            .Produces<SettlementDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{settlementId:guid}/lines", AddLineAsync)
            .WithName("AddSettlementLine")
            .WithSummary("Adds a line to an existing Draft (or reopened) settlement.")
            .RequireAuthorization(EntraAuthorizationPolicies.ForRole(UserRole.Spender))
            .Produces<SettlementDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{settlementId:guid}/submit", SubmitSettlementAsync)
            .WithName("SubmitSettlement")
            .WithSummary("Submits a Draft (or reopened) settlement for approval.")
            .RequireAuthorization(EntraAuthorizationPolicies.ForRole(UserRole.Spender))
            .Produces<SettlementDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // VS5: registered before VS4 so "mine" is resolved as a literal path segment,
        // not as a candidate for the {id:guid} constraint. The constraint would reject it
        // anyway ("mine" is not a Guid), but the explicit ordering makes intent clear.
        group.MapGet("/mine", GetMySettlementsAsync)
            .WithName("GetMySettlements")
            .WithSummary("Returns all settlements belonging to the calling spender.")
            .RequireAuthorization(EntraAuthorizationPolicies.ForRole(UserRole.Spender))
            .Produces<IReadOnlyList<SettlementSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/inbox", GetApproverInboxAsync)
            .WithName("GetApproverInbox")
            .WithSummary("Returns Submitted settlements awaiting decision by the calling approver.")
            .RequireAuthorization(EntraAuthorizationPolicies.ForRole(UserRole.Approver))
            .Produces<IReadOnlyList<SettlementDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{settlementId:guid}", GetSettlementByIdAsync)
            .WithName("GetSettlementById")
            .WithSummary("Returns the full detail of a single settlement (ownership/role checked).")
            .RequireAuthorization(EntraAuthorizationPolicies.ViewSettlement)
            .Produces<SettlementDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{settlementId:guid}/lines/{lineId:guid}", UpdateLineAsync)
            .WithName("UpdateSettlementLine")
            .WithSummary("Updates a line on a Draft settlement.")
            .RequireAuthorization(EntraAuthorizationPolicies.ForRole(UserRole.Spender))
            .Produces<SettlementDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // VS12: PUT /settlements/{settlementId} — update header (date + purpose) of a Draft settlement.
        group.MapPut("/{settlementId:guid}", UpdateHeaderAsync)
            .WithName("UpdateSettlementHeader")
            .WithSummary("Updates the date and purpose of a Draft settlement.")
            .RequireAuthorization(EntraAuthorizationPolicies.ForRole(UserRole.Spender))
            .Produces<SettlementDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{settlementId:guid}/lines/{lineId:guid}", RemoveLineAsync)
            .WithName("RemoveSettlementLine")
            .WithSummary("Removes a line from a Draft settlement.")
            .RequireAuthorization(EntraAuthorizationPolicies.ForRole(UserRole.Spender))
            .Produces<SettlementDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Manager Workflow (VS8 / VS9 / VS10)

        group.MapPost("/{settlementId:guid}/approve", ApproveSettlementAsync)
            .WithName("ApproveSettlement")
            .WithSummary("Approves a Submitted settlement (Approver or System role required).")
            .RequireAuthorization(EntraAuthorizationPolicies.ApproveOrReject)
            .Produces<SettlementDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{settlementId:guid}/reject", RejectSettlementAsync)
            .WithName("RejectSettlement")
            .WithSummary("Rejects a Submitted settlement with a mandatory comment (Approver or System role required).")
            .RequireAuthorization(EntraAuthorizationPolicies.ApproveOrReject)
            .Produces<SettlementDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{settlementId:guid}/reopen", ReopenSettlementAsync)
            .WithName("ReopenSettlement")
            .WithSummary("Reopens a Rejected settlement for editing (spender/owner only). Rejected to Draft, Version++.")
            .RequireAuthorization(EntraAuthorizationPolicies.ForRole(UserRole.Spender))
            .Produces<SettlementDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // VS11: Power Automate callback after D365FO journal creation (Guide §6.2, D-006).
        // System role only — EnsureCanRecordJournal enforces this.
        group.MapPost("/{settlementId:guid}/journal", RecordJournalAsync)
            .WithName("RecordJournal")
            .WithSummary("Records the D365FO journal batch number written back by Power Automate (System role only). Approved \u2192 Journalled.")
            .RequireAuthorization(EntraAuthorizationPolicies.ForRole(UserRole.System))
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

    // ── VS5: GET /settlements/mine ────────────────────────────────────────────────────

    private static async Task<IResult> GetMySettlementsAsync(
        IQueryHandler<GetMySettlementsQuery, IReadOnlyList<SettlementSummaryDto>> handler,
        CancellationToken cancellationToken)
    {
        // No per-query validator exists (and none is needed) — the query carries no
        // client-supplied parameters; identity is always resolved server-side from
        // ICurrentUserContext, consistent with D-016. The repository query is already
        // self-scoped to the caller's UserId inside the handler.
        IReadOnlyList<SettlementSummaryDto> summaries = await handler.HandleAsync(
            new GetMySettlementsQuery(), cancellationToken);

        return Results.Ok(summaries);
    }

    // ── Manager Inbox VS1: GET /settlements/inbox ─────────────────────────────────────

    private static async Task<IResult> GetApproverInboxAsync(
        IQueryHandler<GetApproverInboxQuery, IReadOnlyList<SettlementDto>> handler,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SettlementDto> settlements = await handler.HandleAsync(
            new GetApproverInboxQuery(), cancellationToken);

        return Results.Ok(settlements);
    }

    // ── VS4: GET /settlements/{id} ────────────────────────────────────────────────────

    private static async Task<IResult> GetSettlementByIdAsync(
        Guid settlementId,
        IQueryHandler<GetSettlementByIdQuery, SettlementDto> handler,
        CancellationToken cancellationToken)
    {
        // Same: no validator for a query with a route-bound Guid. The handler throws
        // NotFoundException (→ 404) or ForbiddenException (→ 403) as needed.
        SettlementDto dto = await handler.HandleAsync(
            new GetSettlementByIdQuery(settlementId), cancellationToken);

        return Results.Ok(dto);
    }

    // ── VS6: PUT /settlements/{id}/lines/{lineId} ─────────────────────────────────────

    private static async Task<IResult> UpdateLineAsync(
        Guid settlementId,
        Guid lineId,
        UpdateSettlementLineRequest request,
        ICommandHandler<UpdateLineCommand, SettlementDto> handler,
        IValidator<UpdateLineCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLineCommand(
            settlementId,
            lineId,
            request.CategoryCode,
            request.GrossAmount,
            request.IsVat,
            request.Notes,
            request.CarPlate,
            request.OdometerKm);

        // Same explicit-validation pattern as AddLine/Submit (D-040).
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new PettyCash.Application.Exceptions.ValidationException(
                validationResult.Errors.Select(e => e.ErrorMessage));
        }

        SettlementDto dto = await handler.HandleAsync(command, cancellationToken);

        // 200 with the updated SettlementDto — same reasoning as AddLine (D-042/D-003).
        return Results.Ok(dto);
    }

    // ── VS7: DELETE /settlements/{id}/lines/{lineId} ──────────────────────────────────

    private static async Task<IResult> RemoveLineAsync(
        Guid settlementId,
        Guid lineId,
        ICommandHandler<RemoveLineCommand, SettlementDto> handler,
        IValidator<RemoveLineCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new RemoveLineCommand(settlementId, lineId);

        // RemoveLineCommandValidator only checks both Guids are non-empty — route binding
        // already guarantees well-formed Guids via :guid constraint, so this will never
        // fail in practice. Keeping explicit validation for consistency with D-040.
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        SettlementDto dto = await handler.HandleAsync(command, cancellationToken);

        // 200 with the updated SettlementDto (now without the removed line).
        return Results.Ok(dto);
    }

    // ── VS8: POST /settlements/{id}/approve ────────────────────────────────────

    private static async Task<IResult> ApproveSettlementAsync(
        Guid settlementId,
        ICommandHandler<ApproveSettlementCommand, SettlementDto> handler,
        IValidator<ApproveSettlementCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new ApproveSettlementCommand(settlementId);

        // ApproveSettlementCommandValidator only checks SettlementId non-empty — same
        // reasoning as SubmitSettlement (D-040): the real business rule (must be Submitted)
        // lives in Settlement.Approve() and maps to 400 via the D-031 namespace match.
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        SettlementDto dto = await handler.HandleAsync(command, cancellationToken);

        // 200 — Approve transitions an existing Settlement's status; no new resource (D-042).
        return Results.Ok(dto);
    }

    // ── VS9: POST /settlements/{id}/reject ─────────────────────────────────────

    private static async Task<IResult> RejectSettlementAsync(
        Guid settlementId,
        RejectSettlementRequest request,
        ICommandHandler<RejectSettlementCommand, SettlementDto> handler,
        IValidator<RejectSettlementCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new RejectSettlementCommand(settlementId, request.Comment);

        // RejectSettlementCommandValidator checks SettlementId non-empty AND Comment
        // non-empty + MaximumLength(1000). Settlement.Reject() also checks the comment,
        // but FluentValidation catches it first with the standard error shape (D-040).
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        SettlementDto dto = await handler.HandleAsync(command, cancellationToken);

        // 200 — same reasoning as Approve (D-042).
        return Results.Ok(dto);
    }

    // ── VS10: POST /settlements/{id}/reopen ──────────────────────────────────

    private static async Task<IResult> ReopenSettlementAsync(
        Guid settlementId,
        ICommandHandler<ReopenSettlementCommand, SettlementDto> handler,
        IValidator<ReopenSettlementCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new ReopenSettlementCommand(settlementId);

        // ReopenSettlementCommandValidator only checks SettlementId non-empty (D-040).
        // Domain enforces the Rejected-only precondition via EnsureStatus; maps to 400 (D-031).
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        SettlementDto dto = await handler.HandleAsync(command, cancellationToken);

        // 200 — Reopen transitions status and increments Version; no new resource (D-042/D-014).
        return Results.Ok(dto);
    }

    // ── VS11: POST /settlements/{id}/journal ─────────────────────────────────

    private static async Task<IResult> RecordJournalAsync(
        Guid settlementId,
        RecordJournalRequest request,
        ICommandHandler<RecordJournalCommand, SettlementDto> handler,
        IValidator<RecordJournalCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new RecordJournalCommand(settlementId, request.JournalBatchNumber);

        // RecordJournalCommandValidator checks both SettlementId and JournalBatchNumber
        // are non-empty (D-040). Authorization (System-role only) is enforced inside the
        // handler via EnsureCanRecordJournal — maps to ForbiddenException → 403.
        // Idempotency (D-018): same journal number on an already-Journalled settlement
        // returns 200 without throwing; a different number on an already-Journalled
        // settlement throws a Domain exception → 400 (D-031 namespace match).
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        SettlementDto dto = await handler.HandleAsync(command, cancellationToken);

        // 200 — RecordJournal transitions Approved → Journalled and stores the batch
        // number; no new resource is created (D-042).
        return Results.Ok(dto);
    }

    // ── VS12: PUT /settlements/{id} ──────────────────────────────────────────────

    private static async Task<IResult> UpdateHeaderAsync(
        Guid settlementId,
        UpdateSettlementHeaderRequest request,
        ICommandHandler<UpdateSettlementHeaderCommand, SettlementDto> handler,
        IValidator<UpdateSettlementHeaderCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSettlementHeaderCommand(
            settlementId,
            request.SettlementDate,
            request.Purpose);

        // Same explicit-validation pattern as all other mutation endpoints (D-040).
        // UpdateSettlementHeaderCommandValidator checks SettlementId non-empty, date
        // non-default, and Purpose non-empty + MaximumLength(500) — same constraints
        // as CreateDraftSettlementCommandValidator so that the create and edit paths
        // enforce identical business rules on header fields.
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        SettlementDto dto = await handler.HandleAsync(command, cancellationToken);

        // 200 with the updated SettlementDto — PUT on an existing resource returns
        // the current representation, not 201 (no new resource created). Same
        // precedent as UpdateLine (D-042).
        return Results.Ok(dto);
    }
}
