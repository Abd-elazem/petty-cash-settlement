using PettyCash.Application.Exceptions;
using PettyCash.Application.Settlements.Queries;
using PettyCash.Application.Tests.Fakes;
using PettyCash.Domain.Settlements;
using Xunit;

namespace PettyCash.Application.Tests.Settlements.Queries;

public class GetSettlementByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ForOwner_ReturnsSettlement()
    {
        var repo = new InMemorySettlementRepository();
        var settlement = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Purpose");
        await repo.AddAsync(settlement);
        var handler = new GetSettlementByIdQueryHandler(repo, FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance);

        var result = await handler.HandleAsync(new GetSettlementByIdQuery(settlement.Id));

        Assert.Equal(settlement.Id, result.RequestId);
    }

    [Fact]
    public async Task Handle_ForAssignedApprover_ReturnsSettlement()
    {
        var repo = new InMemorySettlementRepository();
        var settlement = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Purpose");
        await repo.AddAsync(settlement);
        var handler = new GetSettlementByIdQueryHandler(repo, FakeCurrentUserContext.ForApprover("manager@canex.com"), TestAuthorizationPolicy.Instance);

        var result = await handler.HandleAsync(new GetSettlementByIdQuery(settlement.Id));

        Assert.Equal(settlement.Id, result.RequestId);
    }

    [Fact]
    public async Task Handle_ForUnrelatedSpender_ThrowsForbidden()
    {
        var repo = new InMemorySettlementRepository();
        var settlement = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Purpose");
        await repo.AddAsync(settlement);
        var handler = new GetSettlementByIdQueryHandler(repo, FakeCurrentUserContext.ForSpender("spender-2"), TestAuthorizationPolicy.Instance);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(new GetSettlementByIdQuery(settlement.Id)));
    }

    [Fact]
    public async Task Handle_ForApAccountant_ReturnsSettlement()
    {
        var repo = new InMemorySettlementRepository();
        var settlement = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Purpose");
        await repo.AddAsync(settlement);
        var handler = new GetSettlementByIdQueryHandler(repo, FakeCurrentUserContext.ForApAccountant(), TestAuthorizationPolicy.Instance);

        var result = await handler.HandleAsync(new GetSettlementByIdQuery(settlement.Id));

        Assert.Equal(settlement.Id, result.RequestId);
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFound()
    {
        var handler = new GetSettlementByIdQueryHandler(new InMemorySettlementRepository(), FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(new GetSettlementByIdQuery(Guid.NewGuid())));
    }
}
