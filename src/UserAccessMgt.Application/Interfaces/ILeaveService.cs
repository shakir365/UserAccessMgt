using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Leave;

namespace UserAccessMgt.Application.Interfaces;

public interface ILeaveService
{
    Task<ApiResponse<LeaveRequestDto>> ApplyAsync(CreateLeaveRequest request);
    Task<ApiResponse<LeaveRequestDto>> ApproveAsync(int id, int approverId, ApproveLeaveRequest request);
    Task<ApiResponse<LeaveRequestDto>> GetByIdAsync(int id);
    Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetByUserAsync(int userId);
    Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetPendingAsync();
    Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetAllAsync();
}
