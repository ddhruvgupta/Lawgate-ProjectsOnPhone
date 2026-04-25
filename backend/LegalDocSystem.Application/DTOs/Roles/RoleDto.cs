namespace LegalDocSystem.Application.DTOs.Roles;

/// <summary>Describes a role available for assignment.</summary>
/// <param name="Id">Numeric value of the UserRole enum.</param>
/// <param name="Name">Enum name used in JWT claims and policy checks.</param>
/// <param name="Description">Human-readable description of the role's access level.</param>
/// <param name="IsPlatformRole">
/// True for PlatformAdmin / PlatformSuperAdmin — these are internal Lawgate roles
/// and must not be assigned to customer company users.
/// </param>
public record RoleDto(int Id, string Name, string Description, bool IsPlatformRole);
