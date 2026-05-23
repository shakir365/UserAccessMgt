using System.Text.RegularExpressions;
using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.User;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<UserDto>.Ok(await GetDtoAsync(user.Id), "User updated successfully");
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
        RoleName = user.Role == null ? string.Empty : user.Role.Name
    };

    private Task<UserDto> GetDtoAsync(int id)
        => Task.FromResult(_unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == id)
            .Select(MapToDtoExpression)
            .First());
}
