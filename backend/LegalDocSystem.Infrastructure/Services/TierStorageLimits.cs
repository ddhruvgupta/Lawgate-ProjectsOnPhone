using LegalDocSystem.Domain.Enums;

namespace LegalDocSystem.Infrastructure.Services;

/// <summary>
/// Per-tier storage quota configuration, bound from appsettings "TierStorageLimits".
/// Values are in GB; <see cref="ForTier"/> converts to bytes on demand so the config
/// stays human-readable.
/// </summary>
public sealed class TierStorageLimits
{
    public const string SectionName = "TierStorageLimits";

    public int TrialGb { get; set; } = 1;
    public int BasicGb { get; set; } = 100;
    public int ProfessionalGb { get; set; } = 500;
    public int EnterpriseGb { get; set; } = 2048;

    /// <summary>Returns the quota in bytes for <paramref name="tier"/>.</summary>
    public long ForTier(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Trial        => (long)TrialGb        * 1024 * 1024 * 1024,
        SubscriptionTier.Basic        => (long)BasicGb        * 1024 * 1024 * 1024,
        SubscriptionTier.Professional => (long)ProfessionalGb * 1024 * 1024 * 1024,
        SubscriptionTier.Enterprise   => (long)EnterpriseGb   * 1024 * 1024 * 1024,
        _                             => (long)TrialGb        * 1024 * 1024 * 1024,
    };
}
