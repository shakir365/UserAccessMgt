using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAccessMgt.Api.Authorization;
using UserAccessMgt.Application.DTOs.Leave;
using UserAccessMgt.Application.Interfaces;

namespace UserAccessMgt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeaveController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] CreateLeaveRequest request)
    {
        if (!User.CanAccessOwnUser(request.UserId))
            return Forbid();

        var result = await _leaveService.ApplyAsync(request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("approve/{id}")]
    [Authorize(Roles = $"{CurrentUserExtensions.SuperAdminRole},{CurrentUserExtensions.InstituteAdminRole}")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveLeaveRequest request)
    {
        var approverId = User.GetUserId();
        if (!approverId.HasValue)
            return Unauthorized();

        var result = await _leaveService.ApproveAsync(
            id,
            approverId.Value,
            request,
            User.GetInstituteId(),
            User.IsSuperAdmin());
        if (result.ErrorCode == "FORBIDDEN")
            return Forbid();

        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _leaveService.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        if (!User.CanAccessOwnUser(userId))
            return Forbid();

        var result = await _leaveService.GetByUserAsync(userId);
        return Ok(result);
    }

    [HttpGet("pending")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> GetPending()
    {
        var result = await _leaveService.GetPendingAsync();
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _leaveService.GetAllAsync();
        return Ok(result);
    }
}
