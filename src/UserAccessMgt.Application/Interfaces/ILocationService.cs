using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Location;

namespace UserAccessMgt.Application.Interfaces;

public interface ILocationService
{
    Task<ApiResponse<IEnumerable<DivisionDto>>> GetDivisionsAsync();
    Task<ApiResponse<IEnumerable<DistrictDto>>> GetDistrictsByDivisionAsync(int divisionId);
    Task<ApiResponse<IEnumerable<ThanaDto>>> GetThanasByDistrictAsync(int districtId);
}
