namespace LegalDocSystem.Domain.Enums;

public enum DocumentStatus
{
    /// <summary>
    /// SAS URL generated, but upload not yet confirmed by client
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// File uploaded and available
    /// </summary>
    Active = 2,
    
    /// <summary>
    /// File uploaded but undergoing security scan
    /// </summary>
    Scanning = 3,
    
    /// <summary>
    /// Upload failed or validation failed
    /// </summary>
    Failed = 4
}
