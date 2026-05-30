using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Shift;

namespace UserAccessMgt.Application.Interfaces;

public interface IShiftService
{
    Task<ApiResponse<ShiftDto>> CreateAsync(CreateShiftRequest request);
    Task<ApiResponse<ShiftDto>> GetByIdAsync(int id);
    Task<ApiResponse<IEnumerable<ShiftDto>>> GetAllAsync();
    Task<ApiResponse<ShiftDto>> UpdateAsync(int id, UpdateShiftRequest request);
    Task<ApiResponse<string>> DeleteAsync(int id);
}
