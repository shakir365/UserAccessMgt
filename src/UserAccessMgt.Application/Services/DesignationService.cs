using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Designation;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class DesignationService : IDesignationService
{
    private readonly IUnitOfWork _unitOfWork;

    public DesignationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<DesignationDto>> CreateAsync(CreateDesignationRequest request)
    {
        var existing = await _unitOfWork.Repository<Designation>()
            .FirstOrDefaultAsync(d => d.DesignationCode == request.DesignationCode);

        if (existing is not null)
            return ApiResponse<DesignationDto>.Fail("Designation code already exists", "CODE_EXISTS");

        var designation = new Designation
        {
            DesignationCode = request.DesignationCode,
            DesignationName = request.DesignationName,
            IsActive = true,
            CreateDate = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Designation>().AddAsync(designation);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<DesignationDto>.Ok(MapToDto(designation), "Designation created successfully");
    }

    public async Task<ApiResponse<DesignationDto>> GetByIdAsync(int id)
    {
        var designation = await _unitOfWork.Repository<Designation>().GetByIdAsync(id);

        if (designation is null)
            return ApiResponse<DesignationDto>.Fail("Designation not found", "NOT_FOUND");

        return ApiResponse<DesignationDto>.Ok(MapToDto(designation));
    }

    public async Task<ApiResponse<DesignationDto>> GetByCodeAsync(string designationCode)
    {
        var designation = await _unitOfWork.Repository<Designation>()
            .FirstOrDefaultAsync(d => d.DesignationCode == designationCode);

        if (designation is null)
            return ApiResponse<DesignationDto>.Fail("Designation not found", "NOT_FOUND");

        return ApiResponse<DesignationDto>.Ok(MapToDto(designation));
    }

    public async Task<ApiResponse<IEnumerable<DesignationDto>>> GetAllAsync()
    {
        var designations = await _unitOfWork.Repository<Designation>().GetAllAsync();
        return ApiResponse<IEnumerable<DesignationDto>>.Ok(designations.Select(MapToDto));
    }

    public async Task<ApiResponse<DesignationDto>> UpdateAsync(int id, UpdateDesignationRequest request)
    {
        var designation = await _unitOfWork.Repository<Designation>().GetByIdAsync(id);

        if (designation is null)
            return ApiResponse<DesignationDto>.Fail("Designation not found", "NOT_FOUND");

        if (request.DesignationName is not null)
            designation.DesignationName = request.DesignationName;

        if (request.IsActive.HasValue)
            designation.IsActive = request.IsActive.Value;

        _unitOfWork.Repository<Designation>().Update(designation);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<DesignationDto>.Ok(MapToDto(designation), "Designation updated successfully");
    }

    public async Task<ApiResponse<string>> DeleteAsync(int id)
    {
        var designation = await _unitOfWork.Repository<Designation>().GetByIdAsync(id);

        if (designation is null)
            return ApiResponse<string>.Fail("Designation not found", "NOT_FOUND");

        _unitOfWork.Repository<Designation>().Remove(designation);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("Designation deleted successfully");
    }

    private static DesignationDto MapToDto(Designation designation) => new()
    {
        Id = designation.Id,
        DesignationCode = designation.DesignationCode,
        DesignationName = designation.DesignationName,
        IsActive = designation.IsActive,
        CreateDate = designation.CreateDate
    };
}