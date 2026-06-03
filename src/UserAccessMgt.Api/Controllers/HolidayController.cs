using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAccessMgt.Api.Authorization;
using UserAccessMgt.Application.DTOs.Holiday;
using UserAccessMgt.Application.Interfaces;

namespace UserAccessMgt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidayController : ControllerBase
{
    private readonly IHolidayService _holidayService;

    public HolidayController(IHolidayService holidayService)
    {
        _holidayService = holidayService;
    }

    [HttpPost]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> Create([FromBody] CreateHolidayRequest request)
    {
        var result = await _holidayService.CreateAsync(request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _holidayService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _holidayService.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHolidayRequest request)
    {
        var result = await _holidayService.UpdateAsync(id, request);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _holidayService.DeleteAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }
}
