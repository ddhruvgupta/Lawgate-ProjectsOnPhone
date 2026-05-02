using FluentAssertions;
using LegalDocSystem.Domain.Enums;
using LegalDocSystem.Infrastructure.Services;
using Xunit;

namespace LegalDocSystem.UnitTests.Services;

public class TierStorageLimitsTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // ForTier — default values (GB values baked into property initialisers)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ForTier_Trial_Returns1GbByDefault()
    {
        var sut = new TierStorageLimits();

        sut.ForTier(SubscriptionTier.Trial).Should().Be(1L * 1024 * 1024 * 1024);
    }

    [Fact]
    public void ForTier_Basic_Returns100GbByDefault()
    {
        var sut = new TierStorageLimits();

        sut.ForTier(SubscriptionTier.Basic).Should().Be(100L * 1024 * 1024 * 1024);
    }

    [Fact]
    public void ForTier_Professional_Returns500GbByDefault()
    {
        var sut = new TierStorageLimits();

        sut.ForTier(SubscriptionTier.Professional).Should().Be(500L * 1024 * 1024 * 1024);
    }

    [Fact]
    public void ForTier_Enterprise_Returns2TbByDefault()
    {
        var sut = new TierStorageLimits();

        sut.ForTier(SubscriptionTier.Enterprise).Should().Be(2048L * 1024 * 1024 * 1024);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ForTier — respects overridden config values
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ForTier_Trial_RespectsCustomConfig()
    {
        var sut = new TierStorageLimits { TrialGb = 5 };

        sut.ForTier(SubscriptionTier.Trial).Should().Be(5L * 1024 * 1024 * 1024);
    }

    [Fact]
    public void ForTier_Enterprise_RespectsCustomConfig()
    {
        var sut = new TierStorageLimits { EnterpriseGb = 4096 };

        sut.ForTier(SubscriptionTier.Enterprise).Should().Be(4096L * 1024 * 1024 * 1024);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ForTier — ordering: each higher tier returns a larger quota
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ForTier_EachTierIsLargerThanThePrevious()
    {
        var sut = new TierStorageLimits();

        var trial = sut.ForTier(SubscriptionTier.Trial);
        var basic = sut.ForTier(SubscriptionTier.Basic);
        var professional = sut.ForTier(SubscriptionTier.Professional);
        var enterprise = sut.ForTier(SubscriptionTier.Enterprise);

        trial.Should().BeLessThan(basic);
        basic.Should().BeLessThan(professional);
        professional.Should().BeLessThan(enterprise);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ForTier — unknown enum value falls back to Trial quota
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ForTier_UnknownEnumValue_FallsBackToTrialQuota()
    {
        var sut = new TierStorageLimits();
        var unknownTier = (SubscriptionTier)99;

        sut.ForTier(unknownTier).Should().Be(sut.ForTier(SubscriptionTier.Trial));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Return type is long — no int overflow on large tiers
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ForTier_Enterprise_DoesNotOverflow()
    {
        var sut = new TierStorageLimits { EnterpriseGb = int.MaxValue / 1024 }; // ~2 PB

        var act = () => sut.ForTier(SubscriptionTier.Enterprise);

        act.Should().NotThrow();
        sut.ForTier(SubscriptionTier.Enterprise).Should().BeGreaterThan(0);
    }
}
