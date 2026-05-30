using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Weekend;

namespace UserAccessMgt.Application.Interfaces;

public interface IWeekendService
{
    Task<ApiResponse<WeekendDto>> CreateAsync(CreateWeekendRequest request);
    Task<ApiResponse<WeekendDto>> GetByIdAsync(int id);
    Task<ApiResponse<IEnumerable<WeekendDto>>> GetAllAsync();
    Task<ApiResponse<WeekendDto>> UpdateAsync(int id, UpdateWeekendRequest request);
    Task<ApiResponse<string>> DeleteAsync(int id);
}
