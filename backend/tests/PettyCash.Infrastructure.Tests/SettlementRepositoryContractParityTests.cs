using PettyCash.Application.Abstractions;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;
using PettyCash.Infrastructure.Postgres.Repositories;
using PettyCash.Infrastructure.SharePoint.Repositories;
using PettyCash.Infrastructure.SharePoint.Settlements.Gateways;
using PettyCash.Infrastructure.SharePoint.Settlements.Models;
using Xunit;

namespace PettyCash.Infrastructure.Tests;

[Collection("Postgres")]
public sealed class SettlementRepositoryContractParityTests
{
    private readonly PostgresContainerFixture _postgresFixture;

    public SettlementRepositoryContractParityTests(PostgresContainerFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
    }

    [Theory]
    [InlineData(RepositoryBackend.Postgres)]
    [InlineData(RepositoryBackend.SharePoint)]
    public async Task CreateDraftSettlement_PersistsAndLoads(RepositoryBackend backend)
    {
        await using var harness = CreateHarness(backend);
        var repo = await harness.CreateRepositoryAsync();

        var settlement = Settlement.CreateDraft(
            spenderId: $"spender-{Guid.NewGuid():N}",
            spenderNameSnapshot: "Ahmed Ali",
            workerIdSnapshot: "W-001",
            approverEmailSnapshot: "manager@canex.com",
            settlementDate: new DateOnly(2026, 7, 1),
            purpose: "Travel allowance");

        await repo.AddAsync(settlement);
        var reloaded = await repo.GetByIdAsync(settlement.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(settlement.Id, reloaded!.Id);
        Assert.Equal(SettlementStatus.Draft, reloaded.Status);
        Assert.Equal(1, reloaded.Version);
        Assert.Empty(reloaded.Lines);
    }

    [Theory]
    [InlineData(RepositoryBackend.Postgres)]
    [InlineData(RepositoryBackend.SharePoint)]
    public async Task AddUpdateRemoveLines_PersistsLineState(RepositoryBackend backend)
    {
        await using var harness = CreateHarness(backend);
        var repo = await harness.CreateRepositoryAsync();

        var settlement = Settlement.CreateDraft(
            spenderId: $"spender-{Guid.NewGuid():N}",
            spenderNameSnapshot: "Sara Adel",
            workerIdSnapshot: "W-002",
            approverEmailSnapshot: "manager@canex.com",
            settlementDate: new DateOnly(2026, 7, 2),
            purpose: "Mixed expenses");

        var lineOne = settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:Admin", 120m, true, 14m, false, null, "Stationery");
        var lineTwo = settlement.AddLine("FUEL", "6200", "Dept:Fleet", 500m, false, 14m, true, new OdometerReading("ABC-1234", 45000m), null);
        await repo.AddAsync(settlement);

        settlement.UpdateLine(
            lineOne.LineId,
            "OFFICE_SUPPLIES",
            "6100",
            "Dept:Admin",
            130m,
            true,
            14m,
            false,
            null,
            "Updated");
        settlement.RemoveLine(lineTwo.LineId);
        await repo.UpdateAsync(settlement);

        var reloaded = await repo.GetByIdAsync(settlement.Id);

        Assert.NotNull(reloaded);
        Assert.Single(reloaded!.Lines);
        Assert.Equal("OFFICE_SUPPLIES", reloaded.Lines[0].CategoryCode);
        Assert.Equal(130m, reloaded.Lines[0].GrossAmount.Amount);
        Assert.Equal(130m, reloaded.TotalAmount);
    }

    [Theory]
    [InlineData(RepositoryBackend.Postgres)]
    [InlineData(RepositoryBackend.SharePoint)]
    public async Task SubmitApproveRecordJournal_PersistsWorkflow(RepositoryBackend backend)
    {
        await using var harness = CreateHarness(backend);
        var repo = await harness.CreateRepositoryAsync();

        var settlement = Settlement.CreateDraft(
            spenderId: $"spender-{Guid.NewGuid():N}",
            spenderNameSnapshot: "Nour Ahmed",
            workerIdSnapshot: "W-003",
            approverEmailSnapshot: "manager@canex.com",
            settlementDate: new DateOnly(2026, 7, 3),
            purpose: "Workflow path");
        settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:Ops", 80m, false, 14m, false, null, null);
        await repo.AddAsync(settlement);

        settlement.Submit();
        await repo.UpdateAsync(settlement);
        settlement.Approve();
        await repo.UpdateAsync(settlement);
        settlement.RecordJournal("JRN-2026-0001");
        await repo.UpdateAsync(settlement);

        var reloaded = await repo.GetByIdAsync(settlement.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(SettlementStatus.Journalled, reloaded!.Status);
        Assert.Equal("JRN-2026-0001", reloaded.JournalBatchNumber);
    }

    [Theory]
    [InlineData(RepositoryBackend.Postgres)]
    [InlineData(RepositoryBackend.SharePoint)]
    public async Task RejectThenReopen_PersistsVersionAndStatus(RepositoryBackend backend)
    {
        await using var harness = CreateHarness(backend);
        var repo = await harness.CreateRepositoryAsync();

        var settlement = Settlement.CreateDraft(
            spenderId: $"spender-{Guid.NewGuid():N}",
            spenderNameSnapshot: "Mina Fares",
            workerIdSnapshot: "W-004",
            approverEmailSnapshot: "manager@canex.com",
            settlementDate: new DateOnly(2026, 7, 4),
            purpose: "Reopen path");
        settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:Ops", 90m, false, 14m, false, null, null);
        await repo.AddAsync(settlement);

        settlement.Submit();
        await repo.UpdateAsync(settlement);
        settlement.Reject("Missing details");
        await repo.UpdateAsync(settlement);
        settlement.ReopenForEdit();
        await repo.UpdateAsync(settlement);

        var reloaded = await repo.GetByIdAsync(settlement.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(SettlementStatus.Draft, reloaded!.Status);
        Assert.Equal(2, reloaded.Version);
        Assert.Equal("Missing details", reloaded.ApprovalComment);
    }

    [Theory]
    [InlineData(RepositoryBackend.Postgres)]
    [InlineData(RepositoryBackend.SharePoint)]
    public async Task LoadAggregate_ReturnsCompleteGraph(RepositoryBackend backend)
    {
        await using var harness = CreateHarness(backend);
        var repo = await harness.CreateRepositoryAsync();

        var settlement = Settlement.CreateDraft(
            spenderId: $"spender-{Guid.NewGuid():N}",
            spenderNameSnapshot: "Yara Samy",
            workerIdSnapshot: "W-005",
            approverEmailSnapshot: "manager@canex.com",
            settlementDate: new DateOnly(2026, 7, 5),
            purpose: "Load check");
        settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:Ops", 100m, true, 14m, false, null, "Paper");
        settlement.AddLine("FUEL", "6200", "Dept:Fleet", 200m, false, 14m, true, new OdometerReading("XYZ-9999", 99000m), null);
        await repo.AddAsync(settlement);

        var reloaded = await repo.GetByIdAsync(settlement.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(2, reloaded!.Lines.Count);
        Assert.Equal(300m, reloaded.TotalAmount);
        Assert.Equal(SettlementStatus.Draft, reloaded.Status);
    }

    [Theory]
    [InlineData(RepositoryBackend.Postgres)]
    [InlineData(RepositoryBackend.SharePoint)]
    public async Task ListSettlements_ReturnsOnlySpenderRows(RepositoryBackend backend)
    {
        await using var harness = CreateHarness(backend);
        var repo = await harness.CreateRepositoryAsync();
        var targetSpenderId = $"spender-{Guid.NewGuid():N}";

        var mine = Settlement.CreateDraft(targetSpenderId, "Ali", "W-006", "manager@canex.com", new DateOnly(2026, 7, 6), "Mine");
        var other = Settlement.CreateDraft($"spender-{Guid.NewGuid():N}", "Omar", "W-007", "manager@canex.com", new DateOnly(2026, 7, 6), "Other");
        await repo.AddAsync(mine);
        await repo.AddAsync(other);

        var listed = await repo.GetBySpenderIdAsync(targetSpenderId);

        Assert.Single(listed);
        Assert.Equal("Mine", listed[0].Purpose);
        Assert.Equal(targetSpenderId, listed[0].SpenderId);
    }

    [Theory]
    [InlineData(RepositoryBackend.Postgres)]
    [InlineData(RepositoryBackend.SharePoint)]
    public async Task ApproverInbox_ReturnsOnlySubmittedRowsForApprover(RepositoryBackend backend)
    {
        await using var harness = CreateHarness(backend);
        var repo = await harness.CreateRepositoryAsync();
        var targetApprover = $"manager-{Guid.NewGuid():N}@canex.com";

        var submittedMine = Settlement.CreateDraft(
            spenderId: $"spender-{Guid.NewGuid():N}",
            spenderNameSnapshot: "Ali",
            workerIdSnapshot: "W-009",
            approverEmailSnapshot: targetApprover,
            settlementDate: new DateOnly(2026, 7, 8),
            purpose: "Submitted mine");
        submittedMine.AddLine("OFFICE_SUPPLIES", "6100", "Dept:Ops", 100m, false, 14m, false, null, null);
        submittedMine.Submit();

        var draftMine = Settlement.CreateDraft(
            spenderId: $"spender-{Guid.NewGuid():N}",
            spenderNameSnapshot: "Nour",
            workerIdSnapshot: "W-010",
            approverEmailSnapshot: targetApprover,
            settlementDate: new DateOnly(2026, 7, 8),
            purpose: "Draft mine");

        var submittedOtherApprover = Settlement.CreateDraft(
            spenderId: $"spender-{Guid.NewGuid():N}",
            spenderNameSnapshot: "Mona",
            workerIdSnapshot: "W-011",
            approverEmailSnapshot: "other.manager@canex.com",
            settlementDate: new DateOnly(2026, 7, 8),
            purpose: "Submitted other");
        submittedOtherApprover.AddLine("OFFICE_SUPPLIES", "6100", "Dept:Ops", 120m, false, 14m, false, null, null);
        submittedOtherApprover.Submit();

        await repo.AddAsync(submittedMine);
        await repo.AddAsync(draftMine);
        await repo.AddAsync(submittedOtherApprover);

        var inbox = await repo.GetPendingApprovalByApproverEmailAsync(targetApprover);

        Assert.Single(inbox);
        Assert.Equal("Submitted mine", inbox[0].Purpose);
        Assert.Equal(SettlementStatus.Submitted, inbox[0].Status);
        Assert.Equal(targetApprover, inbox[0].ApproverEmailSnapshot);
    }

    [Theory]
    [InlineData(RepositoryBackend.Postgres)]
    [InlineData(RepositoryBackend.SharePoint)]
    public async Task ConcurrencyConflict_SecondWriterFails(RepositoryBackend backend)
    {
        await using var harness = CreateHarness(backend);
        var seedRepo = await harness.CreateRepositoryAsync();
        var seededSettlement = Settlement.CreateDraft(
            spenderId: $"spender-{Guid.NewGuid():N}",
            spenderNameSnapshot: "Ali",
            workerIdSnapshot: "W-008",
            approverEmailSnapshot: "manager@canex.com",
            settlementDate: new DateOnly(2026, 7, 7),
            purpose: "Concurrency");
        seededSettlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:Ops", 75m, false, 14m, false, null, null);
        seededSettlement.Submit();
        await seedRepo.AddAsync(seededSettlement);

        var repoA = await harness.CreateRepositoryAsync();
        var repoB = await harness.CreateRepositoryAsync();

        var settlementA = await repoA.GetByIdAsync(seededSettlement.Id);
        var settlementB = await repoB.GetByIdAsync(seededSettlement.Id);

        Assert.NotNull(settlementA);
        Assert.NotNull(settlementB);

        settlementA!.Approve();
        await repoA.UpdateAsync(settlementA);

        settlementB!.Reject("Stale view");
        await Assert.ThrowsAsync<ConcurrencyException>(() => repoB.UpdateAsync(settlementB));
    }

    [Theory]
    [InlineData(RepositoryBackend.Postgres)]
    [InlineData(RepositoryBackend.SharePoint)]
    public async Task NotFoundBehavior_GetByIdReturnsNull_AndListReturnsEmpty(RepositoryBackend backend)
    {
        await using var harness = CreateHarness(backend);
        var repo = await harness.CreateRepositoryAsync();

        var missing = await repo.GetByIdAsync(Guid.NewGuid());
        var listed = await repo.GetBySpenderIdAsync($"spender-{Guid.NewGuid():N}");

        Assert.Null(missing);
        Assert.Empty(listed);
    }

    private ISettlementRepositoryHarness CreateHarness(RepositoryBackend backend)
    {
        return backend switch
        {
            RepositoryBackend.Postgres => new PostgresSettlementRepositoryHarness(_postgresFixture),
            RepositoryBackend.SharePoint => new SharePointSettlementRepositoryHarness(),
            _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, null)
        };
    }

    public enum RepositoryBackend
    {
        Postgres = 0,
        SharePoint = 1
    }

    private interface ISettlementRepositoryHarness : IAsyncDisposable
    {
        Task<ISettlementRepository> CreateRepositoryAsync();
    }

    private sealed class PostgresSettlementRepositoryHarness : ISettlementRepositoryHarness
    {
        private readonly PostgresContainerFixture _fixture;
        private readonly List<PettyCash.Infrastructure.Postgres.PettyCashDbContext> _contexts = [];

        public PostgresSettlementRepositoryHarness(PostgresContainerFixture fixture)
        {
            _fixture = fixture;
        }

        public Task<ISettlementRepository> CreateRepositoryAsync()
        {
            var context = _fixture.CreateContext();
            _contexts.Add(context);
            ISettlementRepository repository = new PostgresSettlementRepository(context);
            return Task.FromResult(repository);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var context in _contexts)
            {
                await context.DisposeAsync();
            }
        }
    }

    private sealed class SharePointSettlementRepositoryHarness : ISettlementRepositoryHarness
    {
        private readonly InMemorySharePointStore _store = new();

        public Task<ISettlementRepository> CreateRepositoryAsync()
        {
            var headersGateway = new InMemorySharePointSettlementHeadersGateway(_store);
            var linesGateway = new InMemorySharePointSettlementLinesGateway(_store);
            ISettlementRepository repository = new SharePointSettlementRepository(headersGateway, linesGateway);
            return Task.FromResult(repository);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class InMemorySharePointStore
    {
        private readonly object _sync = new();
        private int _etagCounter;
        private readonly Dictionary<Guid, SharePointSettlementHeaderItem> _headersByRequestId = [];
        private readonly Dictionary<Guid, List<SharePointSettlementLineItem>> _linesByRequestId = [];

        public SharePointSettlementHeaderItem? GetHeader(Guid requestId)
        {
            lock (_sync)
            {
                return _headersByRequestId.TryGetValue(requestId, out var header)
                    ? CloneHeader(header)
                    : null;
            }
        }

        public IReadOnlyList<SharePointSettlementHeaderItem> GetHeadersBySpenderId(string spenderId)
        {
            lock (_sync)
            {
                return _headersByRequestId.Values
                    .Where(h => h.SpenderId == spenderId)
                    .Select(CloneHeader)
                    .ToList();
            }
        }

        public IReadOnlyList<SharePointSettlementHeaderItem> GetPendingApprovalByApproverEmail(string approverEmail)
        {
            lock (_sync)
            {
                return _headersByRequestId.Values
                    .Where(h => string.Equals(h.ApproverEmailSnapshot, approverEmail, StringComparison.OrdinalIgnoreCase))
                    .Where(h => string.Equals(h.Status, SettlementStatus.Submitted.ToString(), StringComparison.OrdinalIgnoreCase))
                    .Select(CloneHeader)
                    .ToList();
            }
        }

        public SharePointSettlementHeaderItem AddHeader(SharePointSettlementHeaderItem header)
        {
            lock (_sync)
            {
                var itemId = Guid.NewGuid().ToString("D");
                var etag = NextEtag();
                var stored = header with { ItemId = itemId, ETag = etag };
                _headersByRequestId[stored.RequestId] = stored;
                return CloneHeader(stored);
            }
        }

        public SharePointSettlementHeaderItem UpdateHeader(SharePointSettlementHeaderItem header, string eTag)
        {
            lock (_sync)
            {
                if (!_headersByRequestId.TryGetValue(header.RequestId, out var existing))
                {
                    throw new NotFoundException(nameof(Settlement), header.RequestId);
                }

                if (!string.Equals(existing.ETag, eTag, StringComparison.Ordinal))
                {
                    throw new ConcurrencyException(nameof(Settlement), header.RequestId);
                }

                var updated = header with
                {
                    ItemId = existing.ItemId,
                    ETag = NextEtag()
                };
                _headersByRequestId[updated.RequestId] = updated;
                return CloneHeader(updated);
            }
        }

        public IReadOnlyList<SharePointSettlementLineItem> GetLines(Guid requestId)
        {
            lock (_sync)
            {
                return _linesByRequestId.TryGetValue(requestId, out var lines)
                    ? lines.Select(CloneLine).ToList()
                    : [];
            }
        }

        public void ReplaceLines(Guid requestId, IReadOnlyList<SharePointSettlementLineItem> lines)
        {
            lock (_sync)
            {
                _linesByRequestId[requestId] = lines
                    .Select(l => l with { ItemId = Guid.NewGuid().ToString("D"), ETag = NextEtag() })
                    .OrderBy(l => l.LineNo)
                    .Select(CloneLine)
                    .ToList();
            }
        }

        private string NextEtag()
        {
            _etagCounter++;
            return $"\"{_etagCounter}\"";
        }

        private static SharePointSettlementHeaderItem CloneHeader(SharePointSettlementHeaderItem source) => source with { };
        private static SharePointSettlementLineItem CloneLine(SharePointSettlementLineItem source) => source with { };
    }

    private sealed class InMemorySharePointSettlementHeadersGateway : ISharePointSettlementHeadersGateway
    {
        private readonly InMemorySharePointStore _store;

        public InMemorySharePointSettlementHeadersGateway(InMemorySharePointStore store)
        {
            _store = store;
        }

        public Task<SharePointSettlementHeaderItem?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.GetHeader(requestId));

        public Task<IReadOnlyList<SharePointSettlementHeaderItem>> GetBySpenderIdAsync(string spenderId, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.GetHeadersBySpenderId(spenderId));

        public Task<IReadOnlyList<SharePointSettlementHeaderItem>> GetPendingApprovalByApproverEmailAsync(
            string approverEmail,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_store.GetPendingApprovalByApproverEmail(approverEmail));

        public Task<SharePointSettlementHeaderItem> AddAsync(SharePointSettlementHeaderItem header, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.AddHeader(header));

        public Task<SharePointSettlementHeaderItem> UpdateAsync(SharePointSettlementHeaderItem header, string eTag, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.UpdateHeader(header, eTag));
    }

    private sealed class InMemorySharePointSettlementLinesGateway : ISharePointSettlementLinesGateway
    {
        private readonly InMemorySharePointStore _store;

        public InMemorySharePointSettlementLinesGateway(InMemorySharePointStore store)
        {
            _store = store;
        }

        public Task<IReadOnlyList<SharePointSettlementLineItem>> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.GetLines(requestId));

        public Task ReplaceForSettlementAsync(Guid requestId, IReadOnlyList<SharePointSettlementLineItem> lines, CancellationToken cancellationToken = default)
        {
            _store.ReplaceLines(requestId, lines);
            return Task.CompletedTask;
        }
    }
}
