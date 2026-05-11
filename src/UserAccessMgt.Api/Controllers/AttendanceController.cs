using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAccessMgt.Application.DTOs.Attendance;
using UserAccessMgt.Application.Interfaces;

namespace UserAccessMgt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAttendanceRequest request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var submittedByUserId))
            return Unauthorized();

        var result = await _attendanceService.CreateAsync(request, submittedByUserId);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("institute/{instituteId}")]
    public async Task<IActionResult> GetByInstitute(int instituteId)
    {
        var result = await _attendanceService.GetByInstituteAsync(instituteId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _attendanceService.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var result = await _attendanceService.GetByUserAsync(userId);
        return Ok(result);
    }

    [HttpGet("date/{date}")]
    public async Task<IActionResult> GetByDate(DateTime date)
    {
        var result = await _attendanceService.GetByDateAsync(date);
        return Ok(result);
    }

    [HttpGet("range")]
    public async Task<IActionResult> GetByUserAndDateRange([FromQuery] int userId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await _attendanceService.GetByUserAndDateRangeAsync(userId, from, to);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAttendanceRequest request)
    {
        var result = await _attendanceService.UpdateAsync(id, request);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _attendanceService.DeleteAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }
}
