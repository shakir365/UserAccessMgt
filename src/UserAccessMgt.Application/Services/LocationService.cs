using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Location;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class LocationService : ILocationService
{
    private readonly IUnitOfWork _unitOfWork;

    public LocationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<ApiResponse<IEnumerable<DivisionDto>>> GetDivisionsAsync()
    {
        var divisions = _unitOfWork.Repository<Division>()
            .Query()
            .OrderBy(d => d.DivisionNameEN)
            .Select(d => new DivisionDto
            {
                DivisionId = d.DivisionId,
                DivisionNameEN = d.DivisionNameEN,
                DivisionNameBN = d.DivisionNameBN
            })
            .ToList()
            .AsEnumerable();

        return Task.FromResult(ApiResponse<IEnumerable<DivisionDto>>.Ok(divisions));
    }

    public Task<ApiResponse<IEnumerable<DistrictDto>>> GetDistrictsByDivisionAsync(int divisionId)
    {
        var districts = _unitOfWork.Repository<District>()
            .Query()
            .Where(d => d.DivisionId == divisionId)
            .OrderBy(d => d.DistrictNameEN)
            .Select(d => new DistrictDto
            {
                DistrictId = d.DistrictId,
                DistrictNameEN = d.DistrictNameEN,
                DistrictNameBN = d.DistrictNameBN,
                DivisionId = d.DivisionId
            })
            .ToList()
            .AsEnumerable();

        return Task.FromResult(ApiResponse<IEnumerable<DistrictDto>>.Ok(districts));
    }

    public Task<ApiResponse<IEnumerable<ThanaDto>>> GetThanasByDistrictAsync(int districtId)
    {
        var thanas = _unitOfWork.Repository<Thana>()
            .Query()
            .Where(t => t.DistrictId == districtId)
            .OrderBy(t => t.ThanaNameEN)
            .Select(t => new ThanaDto
            {
                ThanaId = t.ThanaId,
                ThanaNameEN = t.ThanaNameEN,
                ThanaNameBN = t.ThanaNameBN,
                DistrictId = t.DistrictId
            })
            .ToList()
            .AsEnumerable();

        return Task.FromResult(ApiResponse<IEnumerable<ThanaDto>>.Ok(thanas));
    }
}
