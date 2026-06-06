using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAccessMgt.Api.Authorization;
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
        var submittedByUserId = User.GetUserId();
        if (!submittedByUserId.HasValue)
            return Unauthorized();

        if (!User.IsSuperAdmin() && !User.IsInstituteAdmin() && request.UserId != submittedByUserId.Value)
            return Forbid();

        if (!User.CanAccessInstitute(request.InstituteId))
            return Forbid();

        var result = await _attendanceService.CreateAsync(request, submittedByUserId.Value);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("institute/{instituteId}")]
    public async Task<IActionResult> GetByInstitute(int instituteId)
    {
        if (!User.CanAccessInstitute(instituteId))
            return Forbid();

        var result = await _attendanceService.GetByInstituteAsync(instituteId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
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
        if (!User.CanAccessOwnUser(userId))
            return Forbid();

        var result = await _attendanceService.GetByUserAsync(userId);
        return Ok(result);
    }

    [HttpGet("date/{date}")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> GetByDate(DateTime date)
    {
        var result = await _attendanceService.GetByDateAsync(date);
        return Ok(result);
    }

    [HttpGet("range")]
    public async Task<IActionResult> GetByUserAndDateRange([FromQuery] int userId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (!User.CanAccessOwnUser(userId))
            return Forbid();

        var result = await _attendanceService.GetByUserAndDateRangeAsync(userId, from, to);
        return Ok(result);
    }

    [HttpGet("submission-status")]
    public async Task<IActionResult> GetSubmissionStatus([FromQuery] DateTime? date)
    {
        var result = await _attendanceService.GetSubmissionStatusAsync(date);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAttendanceRequest request)
    {
        var result = await _attendanceService.UpdateAsync(id, request);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _attendanceService.DeleteAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }
}
