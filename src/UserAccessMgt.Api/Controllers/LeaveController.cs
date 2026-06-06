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

    [HttpGet("types")]
    public async Task<IActionResult> GetLeaveTypes()
    {
        var result = await _leaveService.GetLeaveTypesAsync();
        return Ok(result);
    }

    [HttpGet("supervisor/{userId:int}")]
    public async Task<IActionResult> GetSupervisor(int userId)
    {
        if (!User.CanAccessOwnUser(userId))
            return Forbid();

        var result = await _leaveService.GetSupervisorForUserAsync(userId);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
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
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveLeaveRequest request)
    {
        var approverId = User.GetUserId();
        if (!approverId.HasValue)
            return Unauthorized();

        var result = await _leaveService.ApproveAsync(
            id,
            approverId.Value,
            request,
            User.IsSuperAdmin());
        if (result.ErrorCode == "FORBIDDEN")
            return Forbid();

        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("cancel/{id}")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelLeaveRequest request)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _leaveService.CancelAsync(id, userId.Value, request);
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
    public async Task<IActionResult> GetPending()
    {
        var supervisorUserId = User.GetUserId();
        if (!supervisorUserId.HasValue)
            return Unauthorized();

        var result = await _leaveService.GetPendingForSupervisorAsync(supervisorUserId.Value, User.IsSuperAdmin());
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
