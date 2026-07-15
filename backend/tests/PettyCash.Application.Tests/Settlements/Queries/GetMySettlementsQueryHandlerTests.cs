using PettyCash.Application.Settlements.Queries;
using PettyCash.Application.Tests.Fakes;
using PettyCash.Domain.Settlements;
using Xunit;

namespace PettyCash.Application.Tests.Settlements.Queries;

public class GetMySettlementsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyCurrentUsersSettlements()
    {
        var repo = new InMemorySettlementRepository();
        var mine = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Mine");
        var someoneElses = Settlement.CreateDraft("spender-2", "Sara Adel", "W-002", "manager@canex.com", new DateOnly(2026, 7, 1), "Not mine");
        await repo.AddAsync(mine);
        await repo.AddAsync(someoneElses);

        var handler = new GetMySettlementsQueryHandler(repo, FakeCurrentUserContext.ForSpender());
        var result = await handler.HandleAsync(new GetMySettlementsQuery());

        Assert.Single(result);
        Assert.Equal("Mine", result[0].Purpose);
    }

    [Fact]
    public async Task Handle_NoSettlements_ReturnsEmptyList()
    {
        var handler = new GetMySettlementsQueryHandler(new InMemorySettlementRepository(), FakeCurrentUserContext.ForSpender());

        var result = await handler.HandleAsync(new GetMySettlementsQuery());

        Assert.Empty(result);
    }
}
