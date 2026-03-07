using LegalDocSystem.Domain.Enums;

namespace LegalDocSystem.Application.DTOs.Companies;

public class CompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = string.Empty;
    public DateTime? SubscriptionEndDate { get; set; }
    public long StorageUsedBytes { get; set; }
    public long StorageQuotaBytes { get; set; }
}
