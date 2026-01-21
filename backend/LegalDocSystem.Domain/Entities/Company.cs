using LegalDocSystem.Domain.Common;
using LegalDocSystem.Domain.Enums;

namespace LegalDocSystem.Domain.Entities;

/// <summary>
/// Represents a law firm or legal organization (Multi-tenant)
/// </summary>
public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    
    // Subscription details
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Trial;
    public DateTime SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Storage limits
    public long StorageUsedBytes { get; set; }
    public long StorageQuotaBytes { get; set; }
    
    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
