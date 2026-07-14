using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAccessMgt.Api.Authorization;
using UserAccessMgt.Application.DTOs.Institute;
using UserAccessMgt.Application.Interfaces;

namespace UserAccessMgt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = CurrentUserExtensions.SuperAdminRole + "," + CurrentUserExtensions.ManagementRole)]
public class InstituteController : ControllerBase
{
    private readonly IInstituteService _instituteService;

    public InstituteController(IInstituteService instituteService)
    {
        _instituteService = instituteService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInstituteRequest request)
    {
        var result = await _instituteService.CreateAsync(request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _instituteService.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var result = await _instituteService.GetByCodeAsync(code);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        var result = await _instituteService.GetPagedAsync(skip, take);
        return Ok(result);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllInstitutes()
    {
        var result = await _instituteService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("GetInstituteByRole")]
    public async Task<IActionResult> GetInstituteByRole()
    {
        var roleName = User.CanManageInstitutes()
            ? CurrentUserExtensions.SuperAdminRole
            : User.IsInstituteAdmin()
                ? CurrentUserExtensions.InstituteAdminRole
                : string.Empty;

        var result = await _instituteService.GetInstituteByRoleAsync(roleName, User.GetInstituteId());
        if (!result.Success)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(result);

            return StatusCode(StatusCodes.Status403Forbidden, result);
        }

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInstituteRequest request)
    {
        var result = await _instituteService.UpdateAsync(id, request);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _instituteService.DeleteAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }
}
