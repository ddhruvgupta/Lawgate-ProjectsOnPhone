namespace LegalDocSystem.Domain.Enums;

public enum ProjectStatus
{
    /// <summary>
    /// Project is being planned
    /// </summary>
    Planning = 1,
    
    /// <summary>
    /// Project is actively in progress
    /// </summary>
    Active = 2,
    
    /// <summary>
    /// Project is temporarily on hold
    /// </summary>
    OnHold = 3,
    
    /// <summary>
    /// Project is completed
    /// </summary>
    Completed = 4,
    
    /// <summary>
    /// Project has been cancelled
    /// </summary>
    Cancelled = 5,
    
    /// <summary>
    /// Project has been archived
    /// </summary>
    Archived = 6
}
