using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Designation;

namespace UserAccessMgt.Application.Interfaces;

public interface IDesignationService
{
    Task<ApiResponse<DesignationDto>> CreateAsync(CreateDesignationRequest request);
    Task<ApiResponse<DesignationDto>> GetByIdAsync(int id);
    Task<ApiResponse<DesignationDto>> GetByCodeAsync(string designationCode);
    Task<ApiResponse<IEnumerable<DesignationDto>>> GetAllAsync();
    Task<ApiResponse<DesignationDto>> UpdateAsync(int id, UpdateDesignationRequest request);
    Task<ApiResponse<string>> DeleteAsync(int id);
}