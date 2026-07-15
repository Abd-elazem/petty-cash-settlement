using PettyCash.Domain.Common;
using PettyCash.Domain.Exceptions;

namespace PettyCash.Domain.Settlements;

/// <summary>
/// Car plate + odometer reading, required together on fuel-category lines (Guide §5.3).
/// Deliberately has no plausibility/sequential validation against prior readings for the
/// same car in MVP — see ASSUMPTIONS.md A-009 (Medium risk, flagged as a control gap,
/// deferred to the Guide's own "Future" anomaly-detection phase).
/// </summary>
public sealed class OdometerReading : ValueObject
{
    public string CarPlate { get; }
    public decimal OdometerKm { get; }

    public OdometerReading(string carPlate, decimal odometerKm)
    {
        if (string.IsNullOrWhiteSpace(carPlate))
        {
            throw new DomainValidationException("Car plate is required for fuel-category lines.");
        }

        if (odometerKm < 0)
        {
            throw new DomainValidationException("Odometer reading cannot be negative.");
        }

        CarPlate = carPlate.Trim();
        OdometerKm = odometerKm;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CarPlate;
        yield return OdometerKm;
    }
}
