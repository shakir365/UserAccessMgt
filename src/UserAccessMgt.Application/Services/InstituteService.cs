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

        if (request.ThanaId.HasValue && !await ThanaExistsAsync(request.ThanaId.Value))
            return ApiResponse<InstituteDto>.Fail("Thana not found", "THANA_NOT_FOUND");

        var institute = new Institute
        {
            Code = code,
            InstituteNameEN = request.InstituteNameEN.Trim(),
            InstituteNameBN = request.InstituteNameBN.Trim(),
            Address = request.Address?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Email = request.Email?.Trim(),
            LatitudeLongitude = request.LatitudeLongitude?.Trim(),
            ThanaId = request.ThanaId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Institute>().AddAsync(institute);
        await _unitOfWork.SaveChangesAsync();

        var createdInstitute = GetInstituteDtoQuery()
            .FirstOrDefault(i => i.Id == institute.Id);

        return ApiResponse<InstituteDto>.Ok(createdInstitute ?? MapToDto(institute), "Institute created successfully");
    }

    public Task<ApiResponse<InstituteDto>> GetByIdAsync(int id)
    {
        var institute = GetInstituteDtoQuery()
            .FirstOrDefault(i => i.Id == id);
        if (institute is null)
            return Task.FromResult(ApiResponse<InstituteDto>.Fail("Institute not found", "NOT_FOUND"));

        return Task.FromResult(ApiResponse<InstituteDto>.Ok(institute));
    }

    public Task<ApiResponse<InstituteDto>> GetByCodeAsync(string code)
    {
        var institute = GetInstituteDtoQuery()
            .FirstOrDefault(i => i.Code == code);

        if (institute is null)
            return Task.FromResult(ApiResponse<InstituteDto>.Fail("Institute not found", "NOT_FOUND"));

        return Task.FromResult(ApiResponse<InstituteDto>.Ok(institute));
    }

    public Task<ApiResponse<IEnumerable<InstituteDto>>> GetAllAsync()
    {
        var institutes = GetInstituteDtoQuery()
            .OrderBy(i => i.InstituteNameEN)
            .ThenBy(i => i.Code)
            .ToList()
            .AsEnumerable();

        return Task.FromResult(ApiResponse<IEnumerable<InstituteDto>>.Ok(institutes));
    }

    public Task<ApiResponse<PagedInstituteResult>> GetPagedAsync(int skip, int take)
    {
        var safeSkip = Math.Max(skip, 0);
        var safeTake = Math.Clamp(take, 1, 100);
        var query = _unitOfWork.Repository<Institute>().Query();
        var totalCount = query.Count();
        var institutes = GetInstituteDtoQuery()
            .OrderBy(i => i.InstituteNameEN)
            .ThenBy(i => i.Code)
            .Skip(safeSkip)
            .Take(safeTake)
            .ToList()
            .AsEnumerable();

        return Task.FromResult(ApiResponse<PagedInstituteResult>.Ok(new PagedInstituteResult
        {
            Items = institutes,
            TotalCount = totalCount
        }));
    }

    public async Task<ApiResponse<IEnumerable<InstituteDto>>> GetInstituteByRoleAsync(string roleName, int? instituteId)
    {
        if (roleName is "SuperAdmin" or "Management")
        {
            return await GetAllAsync();
        }

        if (roleName != "InstituteAdmin")
        {
            return ApiResponse<IEnumerable<InstituteDto>>.Fail(
                "Only SuperAdmin, Management or InstituteAdmin users can view institutes by role.",
                "ROLE_NOT_ALLOWED");
        }

        if (!instituteId.HasValue)
        {
            return ApiResponse<IEnumerable<InstituteDto>>.Fail(
                "Institute id is missing from the current user token.",
                "INSTITUTE_ID_MISSING");
        }

        var institute = GetInstituteDtoQuery()
            .FirstOrDefault(i => i.Id == instituteId.Value);
        if (institute is null)
        {
            return ApiResponse<IEnumerable<InstituteDto>>.Fail("Institute not found", "NOT_FOUND");
        }

        return ApiResponse<IEnumerable<InstituteDto>>.Ok([institute]);
    }

    public async Task<ApiResponse<InstituteDto>> UpdateAsync(int id, UpdateInstituteRequest request)
    {
        var institute = await _unitOfWork.Repository<Institute>().GetByIdAsync(id);
        if (institute is null)
            return ApiResponse<InstituteDto>.Fail("Institute not found", "NOT_FOUND");

        if (request.Code is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return ApiResponse<InstituteDto>.Fail("Institute code is required", "INSTITUTE_CODE_REQUIRED");

            var code = request.Code.Trim();
            var duplicate = await _unitOfWork.Repository<Institute>()
                .FirstOrDefaultAsync(i => i.Id != id && i.Code == code);
            if (duplicate is not null)
                return ApiResponse<InstituteDto>.Fail("Institute code already exists", "CODE_EXISTS");

            institute.Code = code;
        }

        if (request.ThanaId.HasValue)
        {
            if (!await ThanaExistsAsync(request.ThanaId.Value))
                return ApiResponse<InstituteDto>.Fail("Thana not found", "THANA_NOT_FOUND");

            institute.ThanaId = request.ThanaId.Value;
        }

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

        var updatedInstitute = GetInstituteDtoQuery()
            .FirstOrDefault(i => i.Id == institute.Id);

        return ApiResponse<InstituteDto>.Ok(updatedInstitute ?? MapToDto(institute), "Institute updated successfully");
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

    private IQueryable<InstituteDto> GetInstituteDtoQuery()
        => _unitOfWork.Repository<Institute>()
            .Query()
            .Select(i => new InstituteDto
            {
                Id = i.Id,
                Code = i.Code,
                InstituteNameEN = i.InstituteNameEN,
                InstituteNameBN = i.InstituteNameBN,
                Address = i.Address,
                PhoneNumber = i.PhoneNumber,
                Email = i.Email,
                IsActive = i.IsActive,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                LatitudeLongitude = i.LatitudeLongitude,
                DivisionId = i.Thana == null ? null : (int?)i.Thana.District.DivisionId,
                DivisionNameEN = i.Thana == null ? null : i.Thana.District.Division.DivisionNameEN,
                DivisionNameBN = i.Thana == null ? null : i.Thana.District.Division.DivisionNameBN,
                DistrictId = i.Thana == null ? null : (int?)i.Thana.DistrictId,
                DistrictNameEN = i.Thana == null ? null : i.Thana.District.DistrictNameEN,
                DistrictNameBN = i.Thana == null ? null : i.Thana.District.DistrictNameBN,
                ThanaId = i.ThanaId,
                ThanaNameEN = i.Thana == null ? null : i.Thana.ThanaNameEN,
                ThanaNameBN = i.Thana == null ? null : i.Thana.ThanaNameBN
            });

    private Task<bool> ThanaExistsAsync(int thanaId)
        => Task.FromResult(_unitOfWork.Repository<Thana>()
            .Query()
            .Any(t => t.ThanaId == thanaId));

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
        LatitudeLongitude = institute.LatitudeLongitude,
        DivisionId = institute.Thana?.District?.DivisionId,
        DivisionNameEN = institute.Thana?.District?.Division?.DivisionNameEN,
        DivisionNameBN = institute.Thana?.District?.Division?.DivisionNameBN,
        DistrictId = institute.Thana?.DistrictId,
        DistrictNameEN = institute.Thana?.District?.DistrictNameEN,
        DistrictNameBN = institute.Thana?.District?.DistrictNameBN,
        ThanaId = institute.ThanaId,
        ThanaNameEN = institute.Thana?.ThanaNameEN,
        ThanaNameBN = institute.Thana?.ThanaNameBN
    };
}
