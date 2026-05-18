using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAccessMgt.Api.Authorization;
using UserAccessMgt.Application.DTOs.User;
using UserAccessMgt.Application.Interfaces;

namespace UserAccessMgt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!User.CanAccessOwnUser(id))
            return Forbid();

        var result = await _userService.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("institute/{instituteId}")]
    public async Task<IActionResult> GetAll(int instituteId)
    {
        if (!User.CanAccessInstitute(instituteId))
            return Forbid();

        var result = await _userService.GetAllAsync(instituteId);
        return Ok(result);
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var result = await _userService.UpdateAsync(id, request);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _userService.DeactivateAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost("{id}/activate")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _userService.ActivateAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }
}
