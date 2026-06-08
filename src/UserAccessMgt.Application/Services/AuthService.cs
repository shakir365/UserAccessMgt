using System.Text.RegularExpressions;
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
        var loginId = request.LoginID.Trim();
        var user = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.LoginID == loginId);

        if (user is null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            // Login history generation is temporarily disabled.
            // await RecordLoginHistory(null, loginId, ipAddress, userAgent, false, "Invalid credentials");
            return ApiResponse<TokenResponse>.Fail("Invalid login ID or password", "INVALID_CREDENTIALS");
        }

        if (!user.IsActive)
        {
            // Login history generation is temporarily disabled.
            // await RecordLoginHistory(user.Id, loginId, ipAddress, userAgent, false, "Account deactivated");
            return ApiResponse<TokenResponse>.Fail("Account is deactivated", "ACCOUNT_DEACTIVATED");
        }

        await LoadRoleAsync(user);
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

        // Login history generation is temporarily disabled.
        // await RecordLoginHistory(user.Id, loginId, ipAddress, userAgent, true, null);

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

        await LoadRoleAsync(user);
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

    public async Task<ApiResponse<TokenResponse>> RegisterAsync(RegisterRequest request, int? requesterInstituteId, bool requesterIsSuperAdmin)
    {
        var loginId = request.LoginID.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var mobileNumber = request.MobileNumber.Trim();
        if (!Regex.IsMatch(mobileNumber, @"^01[3-9]\d{8}$"))
        {
            return ApiResponse<TokenResponse>.Fail("MobileNumber must be a valid BD mobile number", "INVALID_MOBILE_NUMBER");
        }

        var existingUser = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.LoginID == loginId || (email != null && u.Email == email));

        if (existingUser is not null)
        {
            return ApiResponse<TokenResponse>.Fail("User with this email or login ID already exists", "USER_EXISTS");
        }

        var institute = await _unitOfWork.Repository<Institute>()
            .FirstOrDefaultAsync(i => i.Code == request.InstituteCode);

        if (institute is null)
        {
            return ApiResponse<TokenResponse>.Fail("Invalid institute code", "INVALID_INSTITUTE");
        }

        if (!requesterIsSuperAdmin && institute.Id != requesterInstituteId)
        {
            return ApiResponse<TokenResponse>.Fail(
                "InstituteAdmin users can register users only for their own institute.",
                "INSTITUTE_ACCESS_DENIED");
        }

        Grade? grade = null;
        if (request.GradeId.HasValue)
        {
            grade = await _unitOfWork.Repository<Grade>().GetByIdAsync(request.GradeId.Value);
            if (grade is null)
            {
                return ApiResponse<TokenResponse>.Fail("Invalid grade", "INVALID_GRADE");
            }
        }

        Designation? designation = null;
        if (request.DesignationId.HasValue)
        {
            designation = await _unitOfWork.Repository<Designation>().GetByIdAsync(request.DesignationId.Value);
            if (designation is null)
            {
                return ApiResponse<TokenResponse>.Fail("Invalid designation", "INVALID_DESIGNATION");
            }
        }

        var defaultRole = await _unitOfWork.Repository<Role>()
            .FirstOrDefaultAsync(r => r.Name == "User");
        if (defaultRole is null)
        {
            return ApiResponse<TokenResponse>.Fail("Default user role is not configured", "ROLE_NOT_CONFIGURED");
        }

        var user = new User
        {
            LoginID = loginId,
            Email = email,
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            MobileNumber = mobileNumber,
            InstituteId = institute.Id,
            RoleId = defaultRole.Id,
            Role = defaultRole,
            GradeId = grade?.Id,
            DesignationId = designation?.Id,
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

    private async Task RecordLoginHistory(int? userId, string? loginId, string? ipAddress, string? userAgent, bool isSuccessful, string? failureReason)
    {
        // Login history generation is temporarily disabled.
        // if (!userId.HasValue)
        // {
        //     var user = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.LoginID == loginId);
        //     userId = user?.Id;
        // }
        //
        // if (userId.HasValue)
        // {
        //     await _unitOfWork.Repository<LoginHistory>().AddAsync(new LoginHistory
        //     {
        //         UserId = userId.Value,
        //         LoginAt = DateTime.UtcNow,
        //         IpAddress = ipAddress,
        //         UserAgent = userAgent,
        //         IsSuccessful = isSuccessful,
        //         FailureReason = failureReason
        //     });
        // }
        await Task.CompletedTask;
    }

    private async Task LoadRoleAsync(User user)
    {
        user.Role = await _unitOfWork.Repository<Role>().GetByIdAsync(user.RoleId)
            ?? new Role { Id = user.RoleId, Name = "User", UserDataViewLevelID = 6 };
    }
}
