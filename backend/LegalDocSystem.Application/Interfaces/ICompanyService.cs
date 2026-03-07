using LegalDocSystem.Application.DTOs.Companies;

namespace LegalDocSystem.Application.Interfaces;

public interface ICompanyService
{
    Task<CompanyDto> GetCompanyAsync(int id);
    Task<CompanyDto> UpdateCompanyAsync(int id, UpdateCompanyDto dto);
}
