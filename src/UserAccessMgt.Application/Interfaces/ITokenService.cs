using System.Security.Claims;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
