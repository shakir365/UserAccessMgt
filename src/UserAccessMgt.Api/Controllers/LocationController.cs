using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public LocationController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("divisions")]
    public IActionResult GetDivisions()
    {
        var divisions = _unitOfWork.Repository<Division>()
            .Query()
            .OrderBy(d => d.DivisionNameEN)
            .Select(d => new
            {
                d.DivisionId,
                d.DivisionNameEN,
                d.DivisionNameBN
            })
            .ToList();

        return Ok(ApiResponse<object>.Ok(divisions));
    }

    [HttpGet("divisions/{divisionId:int}/districts")]
    public IActionResult GetDistricts(int divisionId)
    {
        var districts = _unitOfWork.Repository<District>()
            .Query()
            .Where(d => d.DivisionId == divisionId)
            .OrderBy(d => d.DistrictNameEN)
            .Select(d => new
            {
                d.DistrictId,
                d.DistrictNameEN,
                d.DistrictNameBN,
                d.DivisionId
            })
            .ToList();

        return Ok(ApiResponse<object>.Ok(districts));
    }

    [HttpGet("districts/{districtId:int}/thanas")]
    public IActionResult GetThanas(int districtId)
    {
        var thanas = _unitOfWork.Repository<Thana>()
            .Query()
            .Where(t => t.DistrictId == districtId)
            .OrderBy(t => t.ThanaNameEN)
            .Select(t => new
            {
                t.ThanaId,
                t.ThanaNameEN,
                t.ThanaNameBN,
                t.DistrictId
            })
            .ToList();

        return Ok(ApiResponse<object>.Ok(thanas));
    }
}
