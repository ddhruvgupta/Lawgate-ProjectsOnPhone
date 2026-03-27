namespace LegalDocSystem.Application.DTOs.Admin;

public class CompanyOverviewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
    public int ProjectCount { get; set; }
    public int DocumentCount { get; set; }
    public long StorageUsedBytes { get; set; }
}

public class CompanyDetailDto : CompanyOverviewDto
{
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateTime? SubscriptionEndDate { get; set; }
    public List<CompanyUserDto> Users { get; set; } = new();
    public List<CompanyProjectDto> Projects { get; set; } = new();
}

public class CompanyUserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class CompanyProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CompanyDocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
