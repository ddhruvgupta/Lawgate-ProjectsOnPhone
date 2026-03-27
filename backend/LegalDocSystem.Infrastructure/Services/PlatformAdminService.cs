using LegalDocSystem.Application.DTOs.Admin;
using LegalDocSystem.Application.Interfaces;
using LegalDocSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LegalDocSystem.Infrastructure.Services;

public class PlatformAdminService : IPlatformAdminService
{
    private readonly ApplicationDbContext _context;

    // The Lawgate Platform company is excluded from all customer listings
    private const string PlatformCompanyEmail = "platform@lawgate.io";

    public PlatformAdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CompanyOverviewDto>> GetAllCompaniesAsync()
    {
        return await _context.Companies
            .Where(c => c.Email != PlatformCompanyEmail)
            .Select(c => new CompanyOverviewDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                SubscriptionTier = c.SubscriptionTier.ToString(),
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UserCount = c.Users.Count,
                ProjectCount = c.Projects.Count,
                DocumentCount = c.Projects.SelectMany(p => p.Documents).Count(),
                StorageUsedBytes = c.StorageUsedBytes
            })
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<CompanyDetailDto> GetCompanyDetailAsync(int companyId)
    {
        var company = await _context.Companies
            .Where(c => c.Id == companyId && c.Email != PlatformCompanyEmail)
            .Include(c => c.Users)
            .Include(c => c.Projects)
                .ThenInclude(p => p.Documents)
            .FirstOrDefaultAsync();

        if (company == null) throw new KeyNotFoundException("Company not found");

        return new CompanyDetailDto
        {
            Id = company.Id,
            Name = company.Name,
            Email = company.Email,
            Phone = company.Phone,
            Address = company.Address,
            City = company.City,
            State = company.State,
            Country = company.Country,
            SubscriptionTier = company.SubscriptionTier.ToString(),
            SubscriptionEndDate = company.SubscriptionEndDate,
            IsActive = company.IsActive,
            CreatedAt = company.CreatedAt,
            StorageUsedBytes = company.StorageUsedBytes,
            UserCount = company.Users.Count,
            ProjectCount = company.Projects.Count,
            DocumentCount = company.Projects.SelectMany(p => p.Documents).Count(),
            Users = company.Users.Select(u => new CompanyUserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt
            }).ToList(),
            Projects = company.Projects.Select(p => new CompanyProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                ClientName = p.ClientName,
                Status = p.Status.ToString(),
                DocumentCount = p.Documents.Count,
                CreatedAt = p.CreatedAt
            }).ToList()
        };
    }

    public async Task<IEnumerable<CompanyDocumentDto>> GetCompanyDocumentsAsync(int companyId)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && c.Email != PlatformCompanyEmail);

        if (company == null) throw new KeyNotFoundException("Company not found");

        return await _context.Documents
            .Where(d => d.Project.CompanyId == companyId && !d.IsDeleted)
            .Include(d => d.Project)
            .Include(d => d.UploadedBy)
            .Select(d => new CompanyDocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                DocumentType = d.DocumentType.ToString(),
                FileSizeBytes = d.FileSizeBytes,
                ProjectName = d.Project.Name,
                UploadedBy = d.UploadedBy.Email,
                CreatedAt = d.CreatedAt
            })
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }
}
