using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Department;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<DepartmentDto>> CreateAsync(CreateDepartmentRequest request)
    {
        var existing = await _unitOfWork.Repository<Department>()
            .FirstOrDefaultAsync(d => d.DepartmentCode == request.DepartmentCode);

        if (existing is not null)
            return ApiResponse<DepartmentDto>.Fail("Department code already exists", "CODE_EXISTS");

        var department = new Department
        {
            DepartmentCode = request.DepartmentCode,
            DepartmentName = request.DepartmentName,
            IsActive = true,
            CreateDate = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Department>().AddAsync(department);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<DepartmentDto>.Ok(MapToDto(department), "Department created successfully");
    }

    public async Task<ApiResponse<DepartmentDto>> GetByIdAsync(int id)
    {
        var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id);

        if (department is null)
            return ApiResponse<DepartmentDto>.Fail("Department not found", "NOT_FOUND");

        return ApiResponse<DepartmentDto>.Ok(MapToDto(department));
    }

    public async Task<ApiResponse<DepartmentDto>> GetByCodeAsync(string departmentCode)
    {
        var department = await _unitOfWork.Repository<Department>()
            .FirstOrDefaultAsync(d => d.DepartmentCode == departmentCode);

        if (department is null)
            return ApiResponse<DepartmentDto>.Fail("Department not found", "NOT_FOUND");

        return ApiResponse<DepartmentDto>.Ok(MapToDto(department));
    }

    public async Task<ApiResponse<IEnumerable<DepartmentDto>>> GetAllAsync()
    {
        var departments = await _unitOfWork.Repository<Department>().GetAllAsync();

        return ApiResponse<IEnumerable<DepartmentDto>>
            .Ok(departments.Select(MapToDto));
    }

    public async Task<ApiResponse<DepartmentDto>> UpdateAsync(int id, UpdateDepartmentRequest request)
    {
        var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id);

        if (department is null)
            return ApiResponse<DepartmentDto>.Fail("Department not found", "NOT_FOUND");

        if (request.DepartmentName is not null)
            department.DepartmentName = request.DepartmentName;

        if (request.IsActive.HasValue)
            department.IsActive = request.IsActive.Value;

        _unitOfWork.Repository<Department>().Update(department);

        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<DepartmentDto>
            .Ok(MapToDto(department), "Department updated successfully");
    }

    public async Task<ApiResponse<string>> DeleteAsync(int id)
    {
        var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id);

        if (department is null)
            return ApiResponse<string>.Fail("Department not found", "NOT_FOUND");

        _unitOfWork.Repository<Department>().Remove(department);

        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("Department deleted successfully");
    }

    private static DepartmentDto MapToDto(Department department) => new()
    {
        Id = department.Id,
        DepartmentCode = department.DepartmentCode,
        DepartmentName = department.DepartmentName,
        IsActive = department.IsActive,
        CreateDate = department.CreateDate
    };
}