namespace LegalDocSystem.Domain.Enums;

public enum UserRole
{
    /// <summary>
    /// Company owner with full access to all company data
    /// </summary>
    CompanyOwner = 1,
    
    /// <summary>
    /// Administrator with elevated privileges
    /// </summary>
    Admin = 2,
    
    /// <summary>
    /// Standard user with basic access
    /// </summary>
    User = 3,
    
    /// <summary>
    /// Read-only access
    /// </summary>
    Viewer = 4
}
