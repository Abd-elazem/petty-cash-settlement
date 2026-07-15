namespace PettyCash.Application.Abstractions;

/// <summary>
/// The VAT rate as a configuration value, per Guide §5.3's explicit instruction that the
/// 14% rate must not be hard-coded. Infrastructure implements this by reading appsettings/
/// a config provider; Application and Domain both only ever see the resolved number.
/// </summary>
public interface IVatConfiguration
{
    decimal CurrentRatePercent { get; }
}
