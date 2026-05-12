using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Grade;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class GradeService : IGradeService
{
    private readonly IUnitOfWork _unitOfWork;

    public GradeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<GradeDto>> CreateAsync(CreateGradeRequest request)
    {
        var existing = await _unitOfWork.Repository<Grade>()
            .FirstOrDefaultAsync(g => g.GradeCode == request.GradeCode);

        if (existing is not null)
            return ApiResponse<GradeDto>.Fail("Grade code already exists", "CODE_EXISTS");

        var grade = new Grade
        {
            GradeCode = request.GradeCode,
            GradeName = request.GradeName,
            IsActive = true,
            CreateDate = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Grade>().AddAsync(grade);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<GradeDto>.Ok(MapToDto(grade), "Grade created successfully");
    }

    public async Task<ApiResponse<GradeDto>> GetByIdAsync(int id)
    {
        var grade = await _unitOfWork.Repository<Grade>().GetByIdAsync(id);

        if (grade is null)
            return ApiResponse<GradeDto>.Fail("Grade not found", "NOT_FOUND");

        return ApiResponse<GradeDto>.Ok(MapToDto(grade));
    }

    public async Task<ApiResponse<GradeDto>> GetByCodeAsync(string gradeCode)
    {
        var grade = await _unitOfWork.Repository<Grade>()
            .FirstOrDefaultAsync(g => g.GradeCode == gradeCode);

        if (grade is null)
            return ApiResponse<GradeDto>.Fail("Grade not found", "NOT_FOUND");

        return ApiResponse<GradeDto>.Ok(MapToDto(grade));
    }

    public async Task<ApiResponse<IEnumerable<GradeDto>>> GetAllAsync()
    {
        var grades = await _unitOfWork.Repository<Grade>().GetAllAsync();
        return ApiResponse<IEnumerable<GradeDto>>.Ok(grades.Select(MapToDto));
    }

    public async Task<ApiResponse<GradeDto>> UpdateAsync(int id, UpdateGradeRequest request)
    {
        var grade = await _unitOfWork.Repository<Grade>().GetByIdAsync(id);

        if (grade is null)
            return ApiResponse<GradeDto>.Fail("Grade not found", "NOT_FOUND");

        if (request.GradeName is not null)
            grade.GradeName = request.GradeName;

        if (request.IsActive.HasValue)
            grade.IsActive = request.IsActive.Value;

        _unitOfWork.Repository<Grade>().Update(grade);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<GradeDto>.Ok(MapToDto(grade), "Grade updated successfully");
    }

    public async Task<ApiResponse<string>> DeleteAsync(int id)
    {
        var grade = await _unitOfWork.Repository<Grade>().GetByIdAsync(id);

        if (grade is null)
            return ApiResponse<string>.Fail("Grade not found", "NOT_FOUND");

        _unitOfWork.Repository<Grade>().Remove(grade);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("Grade deleted successfully");
    }

    private static GradeDto MapToDto(Grade grade) => new()
    {
        Id = grade.Id,
        GradeCode = grade.GradeCode,
        GradeName = grade.GradeName,
        IsActive = grade.IsActive,
        CreateDate = grade.CreateDate
    };
}