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
        if (string.IsNullOrWhiteSpace(request.DesignationCode))
            return ApiResponse<DesignationDto>.Fail("Designation code is required", "DESIGNATION_CODE_REQUIRED");

        if (string.IsNullOrWhiteSpace(request.DesignationNameEN))
            return ApiResponse<DesignationDto>.Fail("English designation name is required", "DESIGNATION_NAME_EN_REQUIRED");

        var designationCode = request.DesignationCode.Trim();
        var existing = await _unitOfWork.Repository<Designation>()
            .FirstOrDefaultAsync(d => d.DesignationCode == designationCode);

        if (existing is not null)
            return ApiResponse<DesignationDto>.Fail("Designation code already exists", "CODE_EXISTS");

        var designation = new Designation
        {
            DesignationCode = designationCode,
            DesignationNameEN = request.DesignationNameEN.Trim(),
            DesignationNameBN = request.DesignationNameBN.Trim(),
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
        return ApiResponse<IEnumerable<DesignationDto>>.Ok(designations
            .OrderBy(d => d.DesignationNameEN)
            .ThenBy(d => d.DesignationCode)
            .Select(MapToDto));
    }

    public async Task<ApiResponse<DesignationDto>> UpdateAsync(int id, UpdateDesignationRequest request)
    {
        var designation = await _unitOfWork.Repository<Designation>().GetByIdAsync(id);

        if (designation is null)
            return ApiResponse<DesignationDto>.Fail("Designation not found", "NOT_FOUND");

        if (request.DesignationNameEN is not null)
        {
            if (string.IsNullOrWhiteSpace(request.DesignationNameEN))
                return ApiResponse<DesignationDto>.Fail("English designation name is required", "DESIGNATION_NAME_EN_REQUIRED");

            designation.DesignationNameEN = request.DesignationNameEN.Trim();
        }

        if (request.DesignationNameBN is not null)
            designation.DesignationNameBN = request.DesignationNameBN.Trim();

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
        DesignationNameEN = designation.DesignationNameEN,
        DesignationNameBN = designation.DesignationNameBN,
        IsActive = designation.IsActive,
        CreateDate = designation.CreateDate
    };
}
