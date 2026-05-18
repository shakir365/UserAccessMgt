using System.Security.Claims;

namespace UserAccessMgt.Api.Authorization;

public static class CurrentUserExtensions
{
    public const string SuperAdminRole = "SuperAdmin";
    public const string InstituteAdminRole = "InstituteAdmin";

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

    public static bool IsInstituteAdmin(this ClaimsPrincipal user)
        => user.IsInRole(InstituteAdminRole);

    public static bool CanAccessInstitute(this ClaimsPrincipal user, int instituteId)
        => user.IsSuperAdmin() || user.GetInstituteId() == instituteId;

    public static bool CanAccessOwnUser(this ClaimsPrincipal user, int userId)
        => user.IsSuperAdmin() || user.GetUserId() == userId;
}
