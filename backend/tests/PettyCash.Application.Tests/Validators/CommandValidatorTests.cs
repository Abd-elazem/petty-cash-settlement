using PettyCash.Application.Settlements.Commands;
using Xunit;

namespace PettyCash.Application.Tests.Validators;

public class CommandValidatorTests
{
    [Fact]
    public void CreateDraftSettlementCommandValidator_EmptyPurpose_Fails()
    {
        var validator = new CreateDraftSettlementCommandValidator();

        var result = validator.Validate(new CreateDraftSettlementCommand(new DateOnly(2026, 7, 1), ""));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateDraftSettlementCommandValidator_ValidCommand_Passes()
    {
        var validator = new CreateDraftSettlementCommandValidator();

        var result = validator.Validate(new CreateDraftSettlementCommand(new DateOnly(2026, 7, 1), "Site visit"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AddLineCommandValidator_CarPlateWithoutOdometer_Fails()
    {
        var validator = new AddLineCommandValidator();

        var result = validator.Validate(new AddLineCommand(Guid.NewGuid(), "FUEL", 100m, false, null, "ABC-1234", null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void AddLineCommandValidator_OdometerWithoutCarPlate_Fails()
    {
        var validator = new AddLineCommandValidator();

        var result = validator.Validate(new AddLineCommand(Guid.NewGuid(), "FUEL", 100m, false, null, null, 45000m));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void AddLineCommandValidator_BothOrNeitherProvided_Passes()
    {
        var validator = new AddLineCommandValidator();

        Assert.True(validator.Validate(new AddLineCommand(Guid.NewGuid(), "OFFICE_SUPPLIES", 100m, false, null, null, null)).IsValid);
        Assert.True(validator.Validate(new AddLineCommand(Guid.NewGuid(), "FUEL", 100m, false, null, "ABC-1234", 45000m)).IsValid);
    }

    [Fact]
    public void AddLineCommandValidator_ZeroGrossAmount_Fails()
    {
        var validator = new AddLineCommandValidator();

        var result = validator.Validate(new AddLineCommand(Guid.NewGuid(), "OFFICE_SUPPLIES", 0m, false, null, null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RejectSettlementCommandValidator_EmptyComment_Fails()
    {
        var validator = new RejectSettlementCommandValidator();

        var result = validator.Validate(new RejectSettlementCommand(Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RecordJournalCommandValidator_EmptyJournalNumber_Fails()
    {
        var validator = new RecordJournalCommandValidator();

        var result = validator.Validate(new RecordJournalCommand(Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
    }
}
