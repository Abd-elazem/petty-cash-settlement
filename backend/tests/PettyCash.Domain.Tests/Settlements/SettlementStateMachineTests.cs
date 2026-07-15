using PettyCash.Domain.Exceptions;
using PettyCash.Domain.Settlements;
using Xunit;

namespace PettyCash.Domain.Tests.Settlements;

public class SettlementStateMachineTests
{
    private static Settlement CreateValidDraft()
    {
        return Settlement.CreateDraft(
            spenderId: "spender-1",
            spenderNameSnapshot: "Ahmed Ali",
            workerIdSnapshot: "W-001",
            approverEmailSnapshot: "manager@canex.com",
            settlementDate: new DateOnly(2026, 7, 1),
            purpose: "Site visit expenses");
    }

    private static void AddOneLine(Settlement settlement)
    {
        settlement.AddLine(
            categoryCode: "OFFICE_SUPPLIES",
            expenseMainAccountSnapshot: "6100",
            dimensionDefaultsSnapshot: "Dept:IT",
            grossAmount: 150m,
            isVat: false,
            vatRatePercent: 14m,
            kmRequired: false,
            odometer: null,
            notes: "Printer paper");
    }

    [Fact]
    public void CreateDraft_WithValidData_StartsInDraftWithNoLines()
    {
        var settlement = CreateValidDraft();

        Assert.Equal(SettlementStatus.Draft, settlement.Status);
        Assert.Empty(settlement.Lines);
        Assert.Equal(0m, settlement.TotalAmount);
        Assert.Single(settlement.DomainEvents);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateDraft_WithoutApprover_Throws(string? approver)
    {
        Assert.Throws<DomainValidationException>(() => Settlement.CreateDraft(
            "spender-1", "Ahmed Ali", "W-001", approver!, new DateOnly(2026, 7, 1), "Purpose"));
    }

    [Fact]
    public void Submit_WithNoLines_Throws()
    {
        var settlement = CreateValidDraft();

        Assert.Throws<DomainValidationException>(() => settlement.Submit());
    }

    [Fact]
    public void Submit_WithAtLeastOneLine_MovesToSubmitted()
    {
        var settlement = CreateValidDraft();
        AddOneLine(settlement);

        settlement.Submit();

        Assert.Equal(SettlementStatus.Submitted, settlement.Status);
    }

    [Fact]
    public void AddLine_AfterSubmit_Throws()
    {
        var settlement = CreateValidDraft();
        AddOneLine(settlement);
        settlement.Submit();

        Assert.Throws<InvalidSettlementStateException>(() => AddOneLine(settlement));
    }

    [Fact]
    public void Approve_FromSubmitted_MovesToApproved()
    {
        var settlement = CreateValidDraft();
        AddOneLine(settlement);
        settlement.Submit();

        settlement.Approve();

        Assert.Equal(SettlementStatus.Approved, settlement.Status);
    }

    [Fact]
    public void Approve_FromDraft_Throws()
    {
        var settlement = CreateValidDraft();

        Assert.Throws<InvalidSettlementStateException>(() => settlement.Approve());
    }

    [Fact]
    public void Reject_FromSubmitted_MovesToRejected_NotDraft()
    {
        // Regression test for DECISIONS.md D-014: Rejected must be its own status,
        // not an instant auto-revert to Draft.
        var settlement = CreateValidDraft();
        AddOneLine(settlement);
        settlement.Submit();

        settlement.Reject("Missing original receipt");

        Assert.Equal(SettlementStatus.Rejected, settlement.Status);
        Assert.Equal("Missing original receipt", settlement.ApprovalComment);
    }

    [Fact]
    public void Reject_WithoutComment_Throws()
    {
        var settlement = CreateValidDraft();
        AddOneLine(settlement);
        settlement.Submit();

        Assert.Throws<DomainValidationException>(() => settlement.Reject(""));
    }

    [Fact]
    public void ReopenForEdit_FromRejected_ReturnsToDraftAndIncrementsVersion()
    {
        var settlement = CreateValidDraft();
        AddOneLine(settlement);
        settlement.Submit();
        settlement.Reject("Fix the odometer line");
        var versionBefore = settlement.Version;

        settlement.ReopenForEdit();

        Assert.Equal(SettlementStatus.Draft, settlement.Status);
        Assert.Equal(versionBefore + 1, settlement.Version);
    }

    [Fact]
    public void ReopenForEdit_FromDraft_Throws()
    {
        var settlement = CreateValidDraft();

        Assert.Throws<InvalidSettlementStateException>(() => settlement.ReopenForEdit());
    }

    [Fact]
    public void EditLines_AfterReopen_IsAllowedAgain()
    {
        var settlement = CreateValidDraft();
        AddOneLine(settlement);
        settlement.Submit();
        settlement.Reject("Wrong category");
        settlement.ReopenForEdit();

        AddOneLine(settlement); // should not throw — back in Draft

        Assert.Equal(2, settlement.Lines.Count);
    }

    [Fact]
    public void RecordJournal_FromApproved_MovesToJournalledAndStoresBatchNumber()
    {
        var settlement = CreateValidDraft();
        AddOneLine(settlement);
        settlement.Submit();
        settlement.Approve();

        settlement.RecordJournal("PCASH-000123");

        Assert.Equal(SettlementStatus.Journalled, settlement.Status);
        Assert.Equal("PCASH-000123", settlement.JournalBatchNumber);
    }

    [Fact]
    public void RecordJournal_FromSubmitted_Throws()
    {
        var settlement = CreateValidDraft();
        AddOneLine(settlement);
        settlement.Submit();

        Assert.Throws<InvalidSettlementStateException>(() => settlement.RecordJournal("PCASH-000123"));
    }

    [Fact]
    public void RemoveLine_RenumbersRemainingLines()
    {
        var settlement = CreateValidDraft();
        AddOneLine(settlement);
        AddOneLine(settlement);
        AddOneLine(settlement);
        var middleLineId = settlement.Lines[1].LineId;

        settlement.RemoveLine(middleLineId);

        Assert.Equal(2, settlement.Lines.Count);
        Assert.Equal(1, settlement.Lines[0].LineNo);
        Assert.Equal(2, settlement.Lines[1].LineNo);
    }

    [Fact]
    public void TotalAmount_SumsAllLineGrossAmounts()
    {
        var settlement = CreateValidDraft();
        settlement.AddLine("A", "6100", "Dept:IT", 100m, false, 14m, false, null, null);
        settlement.AddLine("B", "6100", "Dept:IT", 250.50m, false, 14m, false, null, null);

        Assert.Equal(350.50m, settlement.TotalAmount);
    }
}
