using PettyCash.Application.Abstractions;

namespace PettyCash.Application.Tests.Fakes;

public sealed class FakeVatConfiguration : IVatConfiguration
{
    public FakeVatConfiguration(decimal ratePercent = 14m)
    {
        CurrentRatePercent = ratePercent;
    }

    public decimal CurrentRatePercent { get; }
}
