using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
    public async Task<IActionResult> Transfer([FromBody] CreateTransferRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var transferredById))
            return Unauthorized();

        var result = await _userTransferService.TransferAsync(request, transferredById);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}")]
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
        var result = await _userTransferService.GetByUserAsync(userId);
        return Ok(result);
    }

    [HttpGet("institute/{instituteId}")]
    public async Task<IActionResult> GetByInstitute(int instituteId)
    {
        var result = await _userTransferService.GetByInstituteAsync(instituteId);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _userTransferService.GetAllAsync();
        return Ok(result);
    }
}
