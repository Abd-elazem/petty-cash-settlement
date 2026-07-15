namespace PettyCash.Domain.Common;

/// <summary>
/// Base class for value objects: equality by value, not identity. Used by Money,
/// VatBreakdown, OdometerReading. Two value objects with the same components are
/// interchangeable — this matters for tests and for detecting no-op updates.
/// </summary>
public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(c => c?.GetHashCode() ?? 0)
            .Aggregate(17, (current, hash) => current * 31 + hash);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
