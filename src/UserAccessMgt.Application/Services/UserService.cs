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

    public async Task<ApiResponse<UserDto>> GetByIdAsync(int id)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user is null)
            return ApiResponse<UserDto>.Fail("User not found", "NOT_FOUND");

        return ApiResponse<UserDto>.Ok(MapToDto(user));
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync(int instituteId)
    {
        var users = await _unitOfWork.Repository<User>()
            .FindAsync(u => u.InstituteId == instituteId);

        return ApiResponse<IEnumerable<UserDto>>.Ok(users.Select(MapToDto));
    }

    public async Task<ApiResponse<UserDto>> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user is null)
            return ApiResponse<UserDto>.Fail("User not found", "NOT_FOUND");

        if (request.FirstName is not null) user.FirstName = request.FirstName;
        if (request.LastName is not null) user.LastName = request.LastName;
        if (request.PhoneNumber is not null) user.PhoneNumber = request.PhoneNumber;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<UserDto>.Ok(MapToDto(user), "User updated successfully");
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

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
        InstituteName = user.Institute?.Name ?? string.Empty,
        RoleName = user.Role?.Name ?? string.Empty
    };
}
