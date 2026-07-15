using PettyCash.Domain.Exceptions;
using PettyCash.Domain.Settlements;
using Xunit;

namespace PettyCash.Domain.Tests.Settlements;

public class FuelLineValidationTests
{
    private static Settlement CreateValidDraft()
    {
        return Settlement.CreateDraft(
            "spender-1", "Ahmed Ali", "W-001", "manager@canex.com",
            new DateOnly(2026, 7, 1), "Fuel run");
    }

    [Fact]
    public void AddLine_FuelCategoryWithoutOdometer_Throws()
    {
        var settlement = CreateValidDraft();

        Assert.Throws<DomainValidationException>(() => settlement.AddLine(
            "FUEL", "6200", "Dept:Fleet", 500m, false, 14m,
            kmRequired: true, odometer: null, notes: null));
    }

    [Fact]
    public void AddLine_NonFuelCategoryWithOdometer_Throws()
    {
        var settlement = CreateValidDraft();
        var odometer = new OdometerReading("ABC-1234", 45210m);

        Assert.Throws<DomainValidationException>(() => settlement.AddLine(
            "OFFICE_SUPPLIES", "6100", "Dept:IT", 150m, false, 14m,
            kmRequired: false, odometer: odometer, notes: null));
    }

    [Fact]
    public void AddLine_FuelCategoryWithOdometer_Succeeds()
    {
        var settlement = CreateValidDraft();
        var odometer = new OdometerReading("ABC-1234", 45210m);

        var line = settlement.AddLine(
            "FUEL", "6200", "Dept:Fleet", 500m, false, 14m,
            kmRequired: true, odometer: odometer, notes: null);

        Assert.Equal(odometer, line.Odometer);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void OdometerReading_WithoutCarPlate_Throws(string plate)
    {
        Assert.Throws<DomainValidationException>(() => new OdometerReading(plate, 100m));
    }

    [Fact]
    public void OdometerReading_WithNegativeKm_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new OdometerReading("ABC-1234", -1m));
    }
}
