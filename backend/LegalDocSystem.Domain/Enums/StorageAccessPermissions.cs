namespace LegalDocSystem.Domain.Enums;

/// <summary>
/// Provider-agnostic access permissions for generating pre-signed / SAS storage URLs.
/// Map to the provider's native permission type inside the Infrastructure layer only.
/// </summary>
[Flags]
public enum StorageAccessPermissions
{
    None   = 0,
    Read   = 1,
    Write  = 2,
    Create = 4,
    Delete = 8
}
