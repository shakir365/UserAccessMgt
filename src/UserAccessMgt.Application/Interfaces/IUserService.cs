using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.User;

namespace UserAccessMgt.Application.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserDto>> GetByIdAsync(int id);
    Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync(int instituteId);
    Task<ApiResponse<UserDto>> UpdateAsync(int id, UpdateUserRequest request);
    Task<ApiResponse<string>> DeactivateAsync(int id);
    Task<ApiResponse<string>> ActivateAsync(int id);
}
