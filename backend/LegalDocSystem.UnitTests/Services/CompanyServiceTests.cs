using FluentAssertions;
using LegalDocSystem.Application.DTOs.Companies;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Domain.Enums;
using LegalDocSystem.Infrastructure.Data;
using LegalDocSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace LegalDocSystem.UnitTests.Services;

public class CompanyServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly CompanyService _sut;

    public CompanyServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"CompanyServiceTests_{Guid.NewGuid()}")
            .Options;
        _context = new ApplicationDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new CompanyService(_context, _cache);
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Company SeedCompany(
        SubscriptionTier tier = SubscriptionTier.Trial,
        long usedBytes = 0,
        long quotaBytes = 1L * 1024 * 1024 * 1024,
        DateTime? endDate = null)
    {
        var company = new Company
        {
            Name = "Test Firm",
            Email = "firm@test.com",
            Phone = "+1-555-0000",
            Address = "1 Legal Lane",
            City = "Austin",
            State = "TX",
            Country = "USA",
            PostalCode = "78701",
            SubscriptionTier = tier,
            SubscriptionStartDate = DateTime.UtcNow,
            SubscriptionEndDate = endDate,
            IsActive = true,
            StorageUsedBytes = usedBytes,
            StorageQuotaBytes = quotaBytes,
            CreatedBy = "seed"
        };
        _context.Companies.Add(company);
        _context.SaveChanges();
        return company;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetCompanyAsync — happy path
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCompanyAsync_ReturnsCorrectStorageBytes()
    {
        var company = SeedCompany(usedBytes: 500_000_000L, quotaBytes: 1L * 1024 * 1024 * 1024);

        var result = await _sut.GetCompanyAsync(company.Id);

        result.StorageUsedBytes.Should().Be(500_000_000L);
        result.StorageQuotaBytes.Should().Be(1L * 1024 * 1024 * 1024);
    }

    [Fact]
    public async Task GetCompanyAsync_ReturnsSubscriptionTierAsString()
    {
        var company = SeedCompany(tier: SubscriptionTier.Professional);

        var result = await _sut.GetCompanyAsync(company.Id);

        result.SubscriptionTier.Should().Be("Professional");
    }

    [Theory]
    [InlineData(SubscriptionTier.Trial,        "Trial")]
    [InlineData(SubscriptionTier.Basic,        "Basic")]
    [InlineData(SubscriptionTier.Professional, "Professional")]
    [InlineData(SubscriptionTier.Enterprise,   "Enterprise")]
    public async Task GetCompanyAsync_MapsAllTiersToString(SubscriptionTier tier, string expected)
    {
        var company = SeedCompany(tier: tier);

        var result = await _sut.GetCompanyAsync(company.Id);

        result.SubscriptionTier.Should().Be(expected);
    }

    [Fact]
    public async Task GetCompanyAsync_ReturnsSubscriptionEndDate()
    {
        var endDate = DateTime.UtcNow.AddDays(14);
        var company = SeedCompany(endDate: endDate);

        var result = await _sut.GetCompanyAsync(company.Id);

        result.SubscriptionEndDate.Should().BeCloseTo(endDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetCompanyAsync_WhenCompanyNotFound_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.GetCompanyAsync(99999);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Company not found*");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetCompanyAsync — caching
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCompanyAsync_SecondCall_ReturnsCachedDto()
    {
        var company = SeedCompany(usedBytes: 100);

        var first  = await _sut.GetCompanyAsync(company.Id);
        // Mutate the DB directly — bypasses the service
        company.StorageUsedBytes = 999_999;
        await _context.SaveChangesAsync();

        var second = await _sut.GetCompanyAsync(company.Id);

        // Should return the cached (stale) value, not the DB mutation
        second.StorageUsedBytes.Should().Be(first.StorageUsedBytes);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UpdateCompanyAsync — invalidates cache
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateCompanyAsync_InvalidatesCacheSoNextReadIsFresh()
    {
        var company = SeedCompany();
        // Warm the cache
        await _sut.GetCompanyAsync(company.Id);

        var dto = new UpdateCompanyDto
        {
            Name = "Updated Firm", Phone = "+1-555-9999", Address = "99 New St",
            City = "Dallas", State = "TX", Country = "USA", PostalCode = "75201"
        };
        await _sut.UpdateCompanyAsync(company.Id, dto);

        var fresh = await _sut.GetCompanyAsync(company.Id);
        fresh.Name.Should().Be("Updated Firm");
        fresh.City.Should().Be("Dallas");
    }

    [Fact]
    public async Task UpdateCompanyAsync_WhenCompanyNotFound_ThrowsKeyNotFoundException()
    {
        var dto = new UpdateCompanyDto
        {
            Name = "X", Phone = "", Address = "", City = "", State = "", Country = "", PostalCode = ""
        };

        var act = async () => await _sut.UpdateCompanyAsync(99999, dto);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Company not found*");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dto field mapping
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCompanyAsync_DtoContainsAllExpectedFields()
    {
        var company = SeedCompany(
            tier: SubscriptionTier.Basic,
            usedBytes: 200_000_000L,
            quotaBytes: 100L * 1024 * 1024 * 1024);

        var result = await _sut.GetCompanyAsync(company.Id);

        result.Id.Should().Be(company.Id);
        result.Name.Should().Be("Test Firm");
        result.Email.Should().Be("firm@test.com");
        result.Phone.Should().Be("+1-555-0000");
        result.SubscriptionTier.Should().Be("Basic");
        result.StorageUsedBytes.Should().Be(200_000_000L);
        result.StorageQuotaBytes.Should().Be(100L * 1024 * 1024 * 1024);
    }
}
