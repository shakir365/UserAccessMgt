using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Institute;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class InstituteService : IInstituteService
{
    private readonly IUnitOfWork _unitOfWork;

    public InstituteService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<InstituteDto>> CreateAsync(CreateInstituteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return ApiResponse<InstituteDto>.Fail("Institute code is required", "INSTITUTE_CODE_REQUIRED");

        if (string.IsNullOrWhiteSpace(request.InstituteNameEN))
            return ApiResponse<InstituteDto>.Fail("English institute name is required", "INSTITUTE_NAME_EN_REQUIRED");

        var code = request.Code.Trim();
        var existing = await _unitOfWork.Repository<Institute>()
            .FirstOrDefaultAsync(i => i.Code == code);

        if (existing is not null)
            return ApiResponse<InstituteDto>.Fail("Institute code already exists", "CODE_EXISTS");

        var institute = new Institute
        {
            Code = code,
            InstituteNameEN = request.InstituteNameEN.Trim(),
            InstituteNameBN = request.InstituteNameBN.Trim(),
            Address = request.Address?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Email = request.Email?.Trim(),
            LatitudeLongitude = request.LatitudeLongitude?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Institute>().AddAsync(institute);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<InstituteDto>.Ok(MapToDto(institute), "Institute created successfully");
    }

    public async Task<ApiResponse<InstituteDto>> GetByIdAsync(int id)
    {
        var institute = await _unitOfWork.Repository<Institute>().GetByIdAsync(id);
        if (institute is null)
            return ApiResponse<InstituteDto>.Fail("Institute not found", "NOT_FOUND");

        return ApiResponse<InstituteDto>.Ok(MapToDto(institute));
    }

    public async Task<ApiResponse<InstituteDto>> GetByCodeAsync(string code)
    {
        var institute = await _unitOfWork.Repository<Institute>()
            .FirstOrDefaultAsync(i => i.Code == code);

        if (institute is null)
            return ApiResponse<InstituteDto>.Fail("Institute not found", "NOT_FOUND");

        return ApiResponse<InstituteDto>.Ok(MapToDto(institute));
    }

    public async Task<ApiResponse<IEnumerable<InstituteDto>>> GetAllAsync()
    {
        var institutes = await _unitOfWork.Repository<Institute>().GetAllAsync();
        return ApiResponse<IEnumerable<InstituteDto>>.Ok(institutes.Select(MapToDto));
    }

    public async Task<ApiResponse<InstituteDto>> UpdateAsync(int id, UpdateInstituteRequest request)
    {
        var institute = await _unitOfWork.Repository<Institute>().GetByIdAsync(id);
        if (institute is null)
            return ApiResponse<InstituteDto>.Fail("Institute not found", "NOT_FOUND");

        if (request.InstituteNameEN is not null)
        {
            if (string.IsNullOrWhiteSpace(request.InstituteNameEN))
                return ApiResponse<InstituteDto>.Fail("English institute name is required", "INSTITUTE_NAME_EN_REQUIRED");

            institute.InstituteNameEN = request.InstituteNameEN.Trim();
        }

        if (request.InstituteNameBN is not null) institute.InstituteNameBN = request.InstituteNameBN.Trim();
        if (request.Address is not null) institute.Address = request.Address.Trim();
        if (request.PhoneNumber is not null) institute.PhoneNumber = request.PhoneNumber.Trim();
        if (request.Email is not null) institute.Email = request.Email.Trim();
        if (request.LatitudeLongitude is not null) institute.LatitudeLongitude = request.LatitudeLongitude.Trim();
        if (request.IsActive.HasValue) institute.IsActive = request.IsActive.Value;

        institute.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<Institute>().Update(institute);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<InstituteDto>.Ok(MapToDto(institute), "Institute updated successfully");
    }

    public async Task<ApiResponse<string>> DeleteAsync(int id)
    {
        var institute = await _unitOfWork.Repository<Institute>().GetByIdAsync(id);
        if (institute is null)
            return ApiResponse<string>.Fail("Institute not found", "NOT_FOUND");

        _unitOfWork.Repository<Institute>().Remove(institute);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("Institute deleted successfully");
    }

    private static InstituteDto MapToDto(Institute institute) => new()
    {
        Id = institute.Id,
        Code = institute.Code,
        InstituteNameEN = institute.InstituteNameEN,
        InstituteNameBN = institute.InstituteNameBN,
        Address = institute.Address,
        PhoneNumber = institute.PhoneNumber,
        Email = institute.Email,
        IsActive = institute.IsActive,
        CreatedAt = institute.CreatedAt,
        UpdatedAt = institute.UpdatedAt,
        LatitudeLongitude = institute.LatitudeLongitude
    };
}
