using UserAccessMgt.Application.DTOs.Auth;
using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;

    public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _passwordService = passwordService;
    }

    public async Task<ApiResponse<TokenResponse>> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent)
    {
        var user = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            await RecordLoginHistory(null, request.Email, ipAddress, userAgent, false, "Invalid credentials");
            return ApiResponse<TokenResponse>.Fail("Invalid email or password", "INVALID_CREDENTIALS");
        }

        if (!user.IsActive)
        {
            await RecordLoginHistory(user.Id, request.Email, ipAddress, userAgent, false, "Account deactivated");
            return ApiResponse<TokenResponse>.Fail("Account is deactivated", "ACCOUNT_DEACTIVATED");
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = new RefreshToken
        {
            Token = _tokenService.GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshToken);

        user.LastLoginAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);

        await RecordLoginHistory(user.Id, request.Email, ipAddress, userAgent, true, null);

        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<TokenResponse>.Ok(new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt
        }, "Login successful");
    }

    public async Task<ApiResponse<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedToken = await _unitOfWork.Repository<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked);

        if (storedToken is null || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return ApiResponse<TokenResponse>.Fail("Invalid or expired refresh token", "INVALID_REFRESH_TOKEN");
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.IsRevoked = true;
        _unitOfWork.Repository<RefreshToken>().Update(storedToken);

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(storedToken.UserId);
        if (user is null || !user.IsActive)
        {
            return ApiResponse<TokenResponse>.Fail("User not found or deactivated", "USER_INVALID");
        }

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = new RefreshToken
        {
            Token = _tokenService.GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<RefreshToken>().AddAsync(newRefreshToken);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<TokenResponse>.Ok(new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = newRefreshToken.ExpiresAt
        });
    }

    public async Task<ApiResponse<string>> LogoutAsync(string refreshToken)
    {
        var storedToken = await _unitOfWork.Repository<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

        if (storedToken is not null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.IsRevoked = true;
            _unitOfWork.Repository<RefreshToken>().Update(storedToken);
            await _unitOfWork.SaveChangesAsync();
        }

        return ApiResponse<string>.Ok("Logged out successfully");
    }

    public async Task<ApiResponse<TokenResponse>> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.Username);

        if (existingUser is not null)
        {
            return ApiResponse<TokenResponse>.Fail("User with this email or username already exists", "USER_EXISTS");
        }

        var institute = await _unitOfWork.Repository<Institute>()
            .FirstOrDefaultAsync(i => i.Code == request.InstituteCode);

        if (institute is null)
        {
            return ApiResponse<TokenResponse>.Fail("Invalid institute code", "INVALID_INSTITUTE");
        }

        var defaultRole = await _unitOfWork.Repository<Role>()
            .FirstOrDefaultAsync(r => r.Name == "User");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            InstituteId = institute.Id,
            RoleId = defaultRole?.Id ?? 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<User>().AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = new RefreshToken
        {
            Token = _tokenService.GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<TokenResponse>.Ok(new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt
        }, "Registration successful");
    }

    private async Task RecordLoginHistory(int? userId, string? email, string? ipAddress, string? userAgent, bool isSuccessful, string? failureReason)
    {
        if (!userId.HasValue)
        {
            var user = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.Email == email);
            userId = user?.Id;
        }

        if (userId.HasValue)
        {
            await _unitOfWork.Repository<LoginHistory>().AddAsync(new LoginHistory
            {
                UserId = userId.Value,
                LoginAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccessful = isSuccessful,
                FailureReason = failureReason
            });
        }
    }
}
