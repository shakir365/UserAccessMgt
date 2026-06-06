using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.User;
using UserAccessMgt.Application.DTOs.UserSupervisor;

namespace UserAccessMgt.Application.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserDto>> GetByIdAsync(int id);
    Task<ApiResponse<UserDto>> GetByLoginIdAsync(string loginId, int? requesterInstituteId, bool requesterIsSuperAdmin);
    Task<ApiResponse<UserDto>> GetSupervisorByLoginIdAsync(string loginId);
    Task<ApiResponse<UserDirectSupervisorLookupDto>> GetActiveDirectSupervisorByLoginIdAsync(string loginId);
    Task<ApiResponse<UserDirectSupervisorDto>> UserSupervisorSetAsync(UserSupervisorSetRequest request, int? createByUserId);
    Task<ApiResponse<string>> DeleteUserSupervisorSetAsync(int userId);
    Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync(int instituteId);
    Task<ApiResponse<IEnumerable<UserRoleDto>>> GetRolesAsync();
    Task<ApiResponse<UserDto>> UpdateAsync(int id, UpdateUserRequest request);
    Task<ApiResponse<string>> ChangeMyPasswordAsync(int userId, ChangeMyPasswordRequest request);
    Task<ApiResponse<string>> ChangeUserPasswordAsync(int id, ChangeUserPasswordRequest request);
    Task<ApiResponse<string>> DeactivateAsync(int id);
    Task<ApiResponse<string>> ActivateAsync(int id);
}
