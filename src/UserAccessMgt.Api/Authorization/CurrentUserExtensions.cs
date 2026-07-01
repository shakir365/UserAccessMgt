using System.Security.Claims;

namespace UserAccessMgt.Api.Authorization;

public static class CurrentUserExtensions
{
    public const string SuperAdminRole = "SuperAdmin";
    public const string InstituteAdminRole = "InstituteAdmin";
    public const string ManagementRole = "Management";
    public const string SuperAdminOrManagementRoles = SuperAdminRole + "," + ManagementRole;
    public const string AdminRoles = SuperAdminRole + "," + InstituteAdminRole + "," + ManagementRole;

    public static int? GetUserId(this ClaimsPrincipal user)
        => int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
            ? userId
            : null;

    public static int? GetInstituteId(this ClaimsPrincipal user)
        => int.TryParse(user.FindFirst("InstituteId")?.Value, out var instituteId)
            ? instituteId
            : null;

    public static bool IsSuperAdmin(this ClaimsPrincipal user)
        => user.IsInRole(SuperAdminRole);

    public static bool IsManagement(this ClaimsPrincipal user)
        => user.IsInRole(ManagementRole);

    public static bool IsSuperAdminOrManagement(this ClaimsPrincipal user)
        => user.IsSuperAdmin() || user.IsManagement();

    public static bool IsInstituteAdmin(this ClaimsPrincipal user)
        => user.IsInRole(InstituteAdminRole);

    public static bool CanAccessInstitute(this ClaimsPrincipal user, int instituteId)
        => user.IsSuperAdminOrManagement() || user.GetInstituteId() == instituteId;

    public static bool CanAccessOwnUser(this ClaimsPrincipal user, int userId)
        => user.IsSuperAdminOrManagement() || user.GetUserId() == userId;
}
