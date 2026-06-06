using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAccessMgt.Api.Authorization;
using UserAccessMgt.Application.DTOs.User;
using UserAccessMgt.Application.DTOs.UserSupervisor;
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

    [HttpGet("UserByToken")]
    public async Task<IActionResult> UserByToken()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _userService.GetByIdAsync(userId.Value);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("id/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!User.CanAccessOwnUser(id))
            return Forbid();

        var result = await _userService.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("GetSupervisorByLoginID")]
    public async Task<IActionResult> GetSupervisorByLoginID([FromQuery] string LoginID)
    {
        var result = await _userService.GetSupervisorByLoginIdAsync(LoginID);
        if (!result.Success)
        {
            if (result.ErrorCode == "LOGIN_ID_REQUIRED")
                return BadRequest(result);

            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("{loginId}")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole + "," + CurrentUserExtensions.InstituteAdminRole)]
    public async Task<IActionResult> GetByLoginId(string loginId)
    {
        var result = await _userService.GetByLoginIdAsync(loginId, User.GetInstituteId(), User.IsSuperAdmin());
        if (!result.Success)
        {
            if (result.ErrorCode == "INSTITUTE_ACCESS_DENIED")
                return StatusCode(StatusCodes.Status403Forbidden, result);

            return NotFound(result);
        }

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

    [HttpGet("roles")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole + "," + CurrentUserExtensions.InstituteAdminRole)]
    public async Task<IActionResult> GetRoles()
    {
        var result = await _userService.GetRolesAsync();
        return Ok(result);
    }

    [HttpPost("UserSupervisorSet")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> UserSupervisorSet([FromBody] UserSupervisorSetRequest request)
    {
        var result = await _userService.UserSupervisorSetAsync(request, User.GetUserId());
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("UserSupervisorSet")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> GetUserSupervisorSet([FromQuery] string LoginID)
    {
        var result = await _userService.GetActiveDirectSupervisorByLoginIdAsync(LoginID);
        if (!result.Success)
        {
            if (result.ErrorCode == "LOGIN_ID_REQUIRED")
                return BadRequest(result);

            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpDelete("UserSupervisorSet/{userId:int}")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole)]
    public async Task<IActionResult> DeleteUserSupervisorSet(int userId)
    {
        var result = await _userService.DeleteUserSupervisorSetAsync(userId);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    private async Task<IActionResult?> ValidateInstituteAdminUserAccessAsync(int id, string action)
    {
        if (User.IsSuperAdmin())
            return null;

        var existingUser = await _userService.GetByIdAsync(id);
        if (!existingUser.Success)
            return NotFound(existingUser);

        if (existingUser.Data?.InstituteId != User.GetInstituteId())
            return StatusCode(StatusCodes.Status403Forbidden,
                UserAccessMgt.Application.DTOs.Common.ApiResponse<object>.Fail(
                    $"You are not eligible to {action} the user",
                    "INSTITUTE_ACCESS_DENIED"));

        return null;
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole + "," + CurrentUserExtensions.InstituteAdminRole)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        if (!User.IsSuperAdmin())
        {
            if (request.InstituteId.HasValue || request.RoleId.HasValue || request.IsActive.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden,
                    UserAccessMgt.Application.DTOs.Common.ApiResponse<object>.Fail(
                        "You are not eligible to update institute, role or account status",
                        "ROLE_ACCESS_DENIED"));

            var accessResult = await ValidateInstituteAdminUserAccessAsync(id, "update");
            if (accessResult is not null)
                return accessResult;
        }

        var result = await _userService.UpdateAsync(id, request);
        if (!result.Success)
        {
            if (result.ErrorCode is "INVALID_GRADE" or "INVALID_DESIGNATION" or "INVALID_INSTITUTE" or "INVALID_ROLE")
                return BadRequest(result);

            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPatch("password")]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangeMyPasswordRequest request)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _userService.ChangeMyPasswordAsync(userId.Value, request);
        if (!result.Success)
        {
            if (result.ErrorCode is "PASSWORD_MISMATCH" or "INVALID_CURRENT_PASSWORD")
                return BadRequest(result);

            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPatch("{id}/password")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole + "," + CurrentUserExtensions.InstituteAdminRole)]
    public async Task<IActionResult> ChangeUserPassword(int id, [FromBody] ChangeUserPasswordRequest request)
    {
        var accessResult = await ValidateInstituteAdminUserAccessAsync(id, "change password for");
        if (accessResult is not null)
            return accessResult;

        var result = await _userService.ChangeUserPasswordAsync(id, request);
        if (!result.Success)
        {
            if (result.ErrorCode == "PASSWORD_MISMATCH")
                return BadRequest(result);

            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole + "," + CurrentUserExtensions.InstituteAdminRole)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var accessResult = await ValidateInstituteAdminUserAccessAsync(id, "deactivate");
        if (accessResult is not null)
            return accessResult;

        var result = await _userService.DeactivateAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost("{id}/activate")]
    [Authorize(Roles = CurrentUserExtensions.SuperAdminRole + "," + CurrentUserExtensions.InstituteAdminRole)]
    public async Task<IActionResult> Activate(int id)
    {
        var accessResult = await ValidateInstituteAdminUserAccessAsync(id, "activate");
        if (accessResult is not null)
            return accessResult;

        var result = await _userService.ActivateAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }
}
