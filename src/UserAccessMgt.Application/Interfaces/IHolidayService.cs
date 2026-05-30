using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Holiday;

namespace UserAccessMgt.Application.Interfaces;

public interface IHolidayService
{
    Task<ApiResponse<HolidayDto>> CreateAsync(CreateHolidayRequest request);
    Task<ApiResponse<HolidayDto>> GetByIdAsync(int id);
    Task<ApiResponse<IEnumerable<HolidayDto>>> GetAllAsync();
    Task<ApiResponse<HolidayDto>> UpdateAsync(int id, UpdateHolidayRequest request);
    Task<ApiResponse<string>> DeleteAsync(int id);
}
