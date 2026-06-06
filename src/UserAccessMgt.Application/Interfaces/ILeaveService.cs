using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Leave;
using UserAccessMgt.Application.DTOs.User;

namespace UserAccessMgt.Application.Interfaces;

public interface ILeaveService
{
    Task<ApiResponse<IEnumerable<LeaveTypeDto>>> GetLeaveTypesAsync();
    Task<ApiResponse<UserDto>> GetSupervisorForUserAsync(int userId);
    Task<ApiResponse<LeaveRequestDto>> ApplyAsync(CreateLeaveRequest request);
    Task<ApiResponse<LeaveRequestDto>> ApproveAsync(int id, int approverId, ApproveLeaveRequest request, bool isSuperAdmin);
    Task<ApiResponse<LeaveRequestDto>> CancelAsync(int id, int userId, CancelLeaveRequest request);
    Task<ApiResponse<LeaveRequestDto>> GetByIdAsync(int id);
    Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetByUserAsync(int userId);
    Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetPendingForSupervisorAsync(int supervisorUserId, bool isSuperAdmin);
    Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetAllAsync();
}
