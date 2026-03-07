using LegalDocSystem.Application.DTOs.Companies;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Domain.Entities;
using LegalDocSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LegalDocSystem.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly ApplicationDbContext _context;

    public CompanyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyDto> GetCompanyAsync(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null) throw new KeyNotFoundException("Company not found");

        return MapToDto(company);
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
