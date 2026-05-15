using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Department;

namespace UserAccessMgt.Application.Interfaces;

public interface IDepartmentService
{
    Task<ApiResponse<DepartmentDto>> CreateAsync(CreateDepartmentRequest request);
    Task<ApiResponse<DepartmentDto>> GetByIdAsync(int id);
    Task<ApiResponse<DepartmentDto>> GetByCodeAsync(string departmentCode);
    Task<ApiResponse<IEnumerable<DepartmentDto>>> GetAllAsync();
    Task<ApiResponse<DepartmentDto>> UpdateAsync(int id, UpdateDepartmentRequest request);
    Task<ApiResponse<string>> DeleteAsync(int id);
}