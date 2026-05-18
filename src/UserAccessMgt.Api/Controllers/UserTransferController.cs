using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAccessMgt.Api.Authorization;
using UserAccessMgt.Application.DTOs.Transfer;
using UserAccessMgt.Application.Interfaces;

namespace UserAccessMgt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserTransferController : ControllerBase
{
    private readonly IUserTransferService _userTransferService;

    public UserTransferController(IUserTransferService userTransferService)
    {
        _userTransferService = userTransferService;
    }

    [HttpPost]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> Transfer([FromBody] CreateTransferRequest request)
    {
        var transferredById = User.GetUserId();
        if (!transferredById.HasValue)
            return Unauthorized();

        var result = await _userTransferService.TransferAsync(request, transferredById.Value);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _userTransferService.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        if (!User.CanAccessOwnUser(userId))
            return Forbid();

        var result = await _userTransferService.GetByUserAsync(userId);
        return Ok(result);
    }

    [HttpGet("institute/{instituteId}")]
    public async Task<IActionResult> GetByInstitute(int instituteId)
    {
        if (!User.CanAccessInstitute(instituteId))
            return Forbid();

        var result = await _userTransferService.GetByInstituteAsync(instituteId);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _userTransferService.GetAllAsync();
        return Ok(result);
    }
}
