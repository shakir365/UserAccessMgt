using UserAccessMgt.Application.DTOs.Auth;
using UserAccessMgt.Application.DTOs.Common;

namespace UserAccessMgt.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<TokenResponse>> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent);
    Task<ApiResponse<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<ApiResponse<string>> LogoutAsync(string refreshToken);
    Task<ApiResponse<TokenResponse>> RegisterAsync(RegisterRequest request, int? requesterInstituteId, bool requesterIsSuperAdmin);
}
