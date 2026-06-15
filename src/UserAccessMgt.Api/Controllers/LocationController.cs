using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAccessMgt.Application.Interfaces;

namespace UserAccessMgt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet("divisions")]
    public async Task<IActionResult> GetDivisions()
    {
        var result = await _locationService.GetDivisionsAsync();
        return Ok(result);
    }

    [HttpGet("divisions/{divisionId:int}/districts")]
    public async Task<IActionResult> GetDistrictsByDivision(int divisionId)
    {
        var result = await _locationService.GetDistrictsByDivisionAsync(divisionId);
        return Ok(result);
    }

    [HttpGet("districts/{districtId:int}/thanas")]
    public async Task<IActionResult> GetThanasByDistrict(int districtId)
    {
        var result = await _locationService.GetThanasByDistrictAsync(districtId);
        return Ok(result);
    }
}
