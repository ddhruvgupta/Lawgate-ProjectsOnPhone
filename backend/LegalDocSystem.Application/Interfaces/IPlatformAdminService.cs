using LegalDocSystem.Application.DTOs.Admin;

namespace LegalDocSystem.Application.Interfaces;

public interface IPlatformAdminService
{
    Task<IEnumerable<CompanyOverviewDto>> GetAllCompaniesAsync();
    Task<CompanyDetailDto> GetCompanyDetailAsync(int companyId);
    Task<IEnumerable<CompanyDocumentDto>> GetCompanyDocumentsAsync(int companyId);
}
