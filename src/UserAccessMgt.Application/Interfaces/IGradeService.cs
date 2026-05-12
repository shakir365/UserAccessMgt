using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Grade;

namespace UserAccessMgt.Application.Interfaces;

public interface IGradeService
{
    Task<ApiResponse<GradeDto>> CreateAsync(CreateGradeRequest request);
    Task<ApiResponse<GradeDto>> GetByIdAsync(int id);
    Task<ApiResponse<GradeDto>> GetByCodeAsync(string gradeCode);
    Task<ApiResponse<IEnumerable<GradeDto>>> GetAllAsync();
    Task<ApiResponse<GradeDto>> UpdateAsync(int id, UpdateGradeRequest request);
    Task<ApiResponse<string>> DeleteAsync(int id);
}