using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        var result = await _leaveService.ApplyAsync(request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("approve/{id}")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveLeaveRequest request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var approverId))
            return Unauthorized();

        var result = await _leaveService.ApproveAsync(id, approverId, request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}")]
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
        var result = await _leaveService.GetByUserAsync(userId);
        return Ok(result);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var result = await _leaveService.GetPendingAsync();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _leaveService.GetAllAsync();
        return Ok(result);
    }
}
