namespace LegalDocSystem.Domain.Enums;

public enum PermissionLevel
{
    /// <summary>
    /// No access to the project
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Read-only access to project documents
    /// </summary>
    Viewer = 1,
    
    /// <summary>
    /// Can view and comment on documents
    /// </summary>
    Commenter = 2,
    
    /// <summary>
    /// Can create and edit documents
    /// </summary>
    Editor = 3,
    
    /// <summary>
    /// Full control over project including user management
    /// </summary>
    Admin = 4
}
