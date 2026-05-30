using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Institute;

namespace UserAccessMgt.Application.Interfaces;

public interface IInstituteService
{
    Task<ApiResponse<InstituteDto>> CreateAsync(CreateInstituteRequest request);
    Task<ApiResponse<InstituteDto>> GetByIdAsync(int id);
    Task<ApiResponse<InstituteDto>> GetByCodeAsync(string code);
    Task<ApiResponse<IEnumerable<InstituteDto>>> GetAllAsync();
    Task<ApiResponse<PagedInstituteResult>> GetPagedAsync(int skip, int take);
    Task<ApiResponse<IEnumerable<InstituteDto>>> GetInstituteByRoleAsync(string roleName, int? instituteId);
    Task<ApiResponse<InstituteDto>> UpdateAsync(int id, UpdateInstituteRequest request);
    Task<ApiResponse<string>> DeleteAsync(int id);
}
