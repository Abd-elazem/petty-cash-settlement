using PettyCash.Application.Exceptions;
using PettyCash.Application.Settlements.Commands;
using PettyCash.Application.Tests.Fakes;
using Xunit;

namespace PettyCash.Application.Tests.Settlements.Commands;

public class CreateDraftSettlementCommandHandlerTests
{
    [Fact]
    public async Task Handle_ForActiveSpender_CreatesDraftAndAudits()
    {
        var userProfiles = new InMemoryAppUserProfileRepository()
            .Seed(new(AppUserId: "spender-1", DisplayName: "Ahmed Ali", WorkerId: "W-001", ApproverEmail: "manager@canex.com", Active: true));
        var settlements = new InMemorySettlementRepository();
        var audit = new FakeAuditLogger();
        var handler = new CreateDraftSettlementCommandHandler(
            settlements, userProfiles, FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance, audit);

        var result = await handler.HandleAsync(new CreateDraftSettlementCommand(new DateOnly(2026, 7, 1), "Site visit"));

        Assert.Equal("Draft", result.Status);
        Assert.Equal("Ahmed Ali", result.SpenderName);
        Assert.Equal("manager@canex.com", result.ApproverEmail);
        Assert.Single(audit.Entries);
        Assert.Equal("Created", audit.Entries[0].Action);
    }

    [Fact]
    public async Task Handle_ForDeactivatedSpender_Throws()
    {
        var userProfiles = new InMemoryAppUserProfileRepository()
            .Seed(new(AppUserId: "spender-1", DisplayName: "Ahmed Ali", WorkerId: "W-001", ApproverEmail: "manager@canex.com", Active: false));
        var handler = new CreateDraftSettlementCommandHandler(
            new InMemorySettlementRepository(), userProfiles, FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance, new FakeAuditLogger());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.HandleAsync(new CreateDraftSettlementCommand(new DateOnly(2026, 7, 1), "Site visit")));
    }

    [Fact]
    public async Task Handle_ForUnknownSpender_ThrowsNotFound()
    {
        var handler = new CreateDraftSettlementCommandHandler(
            new InMemorySettlementRepository(), new InMemoryAppUserProfileRepository(),
            FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance, new FakeAuditLogger());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(new CreateDraftSettlementCommand(new DateOnly(2026, 7, 1), "Site visit")));
    }

    [Fact]
    public async Task Handle_ForNonSpenderRole_ThrowsForbidden()
    {
        var handler = new CreateDraftSettlementCommandHandler(
            new InMemorySettlementRepository(), new InMemoryAppUserProfileRepository(),
            FakeCurrentUserContext.ForApprover(), TestAuthorizationPolicy.Instance, new FakeAuditLogger());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.HandleAsync(new CreateDraftSettlementCommand(new DateOnly(2026, 7, 1), "Site visit")));
    }
}
