using Microsoft.Extensions.Configuration;
using PettyCash.Application.Abstractions;

namespace PettyCash.Infrastructure.Postgres;

/// <summary>
/// Reads the VAT rate from configuration (Guide §5.3's explicit "must not be hard-coded"
/// requirement). Not Postgres-specific — it's grouped under Postgres/ only because this
/// sprint has nowhere else to put it yet; it would move if Infrastructure grows enough
/// subfolders to warrant a dedicated Configuration/ area.
/// </summary>
public sealed class ConfigurationVatConfiguration : IVatConfiguration
{
    private const decimal DefaultRatePercent = 14m; // Egyptian standard rate at time of writing — a fallback only, never relied on silently in production config.

    public ConfigurationVatConfiguration(IConfiguration configuration)
    {
        CurrentRatePercent = configuration.GetValue<decimal?>("PettyCash:VatRatePercent") ?? DefaultRatePercent;
    }

    public decimal CurrentRatePercent { get; }
}
