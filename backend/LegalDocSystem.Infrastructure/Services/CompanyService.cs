using LegalDocSystem.Application.DTOs.Companies;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LegalDocSystem.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    private static string CacheKey(int id) => $"company:{id}";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public CompanyService(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<CompanyDto> GetCompanyAsync(int id)
    {
        if (_cache.TryGetValue(CacheKey(id), out CompanyDto? cached) && cached is not null)
            return cached;

        var company = await _context.Companies.FindAsync(id);
        if (company == null) throw new KeyNotFoundException("Company not found");

        var dto = MapToDto(company);
        _cache.Set(CacheKey(id), dto, CacheDuration);
        return dto;
    }

    public async Task<CompanyDto> UpdateCompanyAsync(int id, UpdateCompanyDto dto)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null) throw new KeyNotFoundException("Company not found");

        company.Name = dto.Name;
        company.Phone = dto.Phone;
        company.Address = dto.Address;
        company.City = dto.City;
        company.State = dto.State;
        company.Country = dto.Country;
        company.PostalCode = dto.PostalCode;

        await _context.SaveChangesAsync();

        // Invalidate cached entry so next read reflects the update
        _cache.Remove(CacheKey(id));

        return MapToDto(company);
    }

    private static CompanyDto MapToDto(Company company)
    {
        return new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Email = company.Email,
            Phone = company.Phone,
            Address = company.Address,
            City = company.City,
            State = company.State,
            Country = company.Country,
            PostalCode = company.PostalCode,
            SubscriptionTier = company.SubscriptionTier.ToString(),
            SubscriptionEndDate = company.SubscriptionEndDate,
            StorageUsedBytes = company.StorageUsedBytes,
            StorageQuotaBytes = company.StorageQuotaBytes
        };
    }
}
