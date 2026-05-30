using System.Text.RegularExpressions;
using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.User;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;

    public UserService(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }

    public Task<ApiResponse<UserDto>> GetByIdAsync(int id)
    {
        var user = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == id)
            .Select(MapToDtoExpression)
            .FirstOrDefault();

        if (user is null)
            return Task.FromResult(ApiResponse<UserDto>.Fail("User not found", "NOT_FOUND"));

        return Task.FromResult(ApiResponse<UserDto>.Ok(user));
    }

    public Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync(int instituteId)
    {
        var users = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.InstituteId == instituteId)
            .Select(MapToDtoExpression)
            .ToList();

        return Task.FromResult(ApiResponse<IEnumerable<UserDto>>.Ok(users.AsEnumerable()));
    }

    public Task<ApiResponse<UserDto>> GetByLoginIdAsync(string loginId, int? requesterInstituteId, bool requesterIsSuperAdmin)
    {
        var normalizedLoginId = loginId.Trim();
        var user = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.LoginID == normalizedLoginId)
            .Select(MapToDtoExpression)
            .FirstOrDefault();

        if (user is null)
            return Task.FromResult(ApiResponse<UserDto>.Fail("User not found", "NOT_FOUND"));

        if (!requesterIsSuperAdmin && user.InstituteId != requesterInstituteId)
        {
            return Task.FromResult(ApiResponse<UserDto>.Fail(
                "You are not eligible to get the user",
                "INSTITUTE_ACCESS_DENIED"));
        }

        return Task.FromResult(ApiResponse<UserDto>.Ok(user));
    }

    public Task<ApiResponse<IEnumerable<UserRoleDto>>> GetRolesAsync()
    {
        var roles = _unitOfWork.Repository<Role>()
            .Query()
            .OrderBy(r => r.Name)
            .Select(r => new UserRoleDto
            {
                Id = r.Id,
                Name = r.Name
            })
            .ToList()
            .AsEnumerable();

        return Task.FromResult(ApiResponse<IEnumerable<UserRoleDto>>.Ok(roles));
    }

    public async Task<ApiResponse<UserDto>> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user is null)
            return ApiResponse<UserDto>.Fail("User not found", "NOT_FOUND");

        if (request.FirstName is not null) user.FirstName = request.FirstName;
        if (request.LastName is not null) user.LastName = request.LastName;
        if (request.MobileNumber is not null)
        {
            var mobileNumber = request.MobileNumber.Trim();
            if (!Regex.IsMatch(mobileNumber, @"^01[3-9]\d{8}$"))
            {
                return ApiResponse<UserDto>.Fail("MobileNumber must be a valid BD mobile number", "INVALID_MOBILE_NUMBER");
            }

            user.MobileNumber = mobileNumber;
        }
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        if (request.InstituteId.HasValue)
        {
            var institute = await _unitOfWork.Repository<Institute>().GetByIdAsync(request.InstituteId.Value);
            if (institute is null)
                return ApiResponse<UserDto>.Fail("Invalid institute", "INVALID_INSTITUTE");

            user.InstituteId = institute.Id;
        }
        if (request.RoleId.HasValue)
        {
            var role = await _unitOfWork.Repository<Role>().GetByIdAsync(request.RoleId.Value);
            if (role is null)
                return ApiResponse<UserDto>.Fail("Invalid role", "INVALID_ROLE");

            user.RoleId = role.Id;
        }
        if (request.GradeId.HasValue)
        {
            var grade = await _unitOfWork.Repository<Grade>().GetByIdAsync(request.GradeId.Value);
            if (grade is null)
                return ApiResponse<UserDto>.Fail("Invalid grade", "INVALID_GRADE");

            user.GradeId = grade.Id;
        }
        if (request.DesignationId.HasValue)
        {
            var designation = await _unitOfWork.Repository<Designation>().GetByIdAsync(request.DesignationId.Value);
            if (designation is null)
                return ApiResponse<UserDto>.Fail("Invalid designation", "INVALID_DESIGNATION");

            user.DesignationId = designation.Id;
        }

        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<UserDto>.Ok(await GetDtoAsync(user.Id), "User updated successfully");
    }

    public async Task<ApiResponse<string>> ChangeMyPasswordAsync(int userId, ChangeMyPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return ApiResponse<string>.Fail("New password and confirm password do not match", "PASSWORD_MISMATCH");

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user is null)
            return ApiResponse<string>.Fail("User not found", "NOT_FOUND");

        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return ApiResponse<string>.Fail("Current password is incorrect", "INVALID_CURRENT_PASSWORD");

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("Password changed successfully");
    }

    public async Task<ApiResponse<string>> ChangeUserPasswordAsync(int id, ChangeUserPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return ApiResponse<string>.Fail("New password and confirm password do not match", "PASSWORD_MISMATCH");

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user is null)
            return ApiResponse<string>.Fail("User not found", "NOT_FOUND");

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("Password changed successfully");
    }

    public async Task<ApiResponse<string>> DeactivateAsync(int id)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user is null)
            return ApiResponse<string>.Fail("User not found", "NOT_FOUND");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("User deactivated successfully");
    }

    public async Task<ApiResponse<string>> ActivateAsync(int id)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user is null)
            return ApiResponse<string>.Fail("User not found", "NOT_FOUND");

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("User activated successfully");
    }

    private static readonly System.Linq.Expressions.Expression<Func<User, UserDto>> MapToDtoExpression = user => new UserDto
    {
        Id = user.Id,
        LoginID = user.LoginID,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        MobileNumber = user.MobileNumber,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
        InstituteId = user.InstituteId,
        InstituteName = user.Institute == null ? string.Empty : user.Institute.InstituteNameEN,
        RoleId = user.RoleId,
        RoleName = user.Role == null ? string.Empty : user.Role.Name,
        GradeId = user.GradeId,
        GradeName = user.Grade == null ? null : user.Grade.GradeNameEN,
        DesignationId = user.DesignationId,
        DesignationName = user.Designation == null ? null : user.Designation.DesignationNameEN
    };

    private Task<UserDto> GetDtoAsync(int id)
        => Task.FromResult(_unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == id)
            .Select(MapToDtoExpression)
            .First());
}
