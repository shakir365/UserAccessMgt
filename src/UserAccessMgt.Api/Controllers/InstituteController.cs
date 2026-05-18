using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAccessMgt.Api.Authorization;
using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Institute;
using UserAccessMgt.Application.Interfaces;

namespace UserAccessMgt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
        if (!User.IsSuperAdmin())
            return SuperAdminRequired("create institutes");

        var result = await _instituteService.CreateAsync(request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!User.CanAccessInstitute(id))
            return Forbid();

        var result = await _instituteService.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        if (!User.IsSuperAdmin())
            return SuperAdminRequired("view institute by code");

        var result = await _instituteService.GetByCodeAsync(code);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!User.IsSuperAdmin())
            return SuperAdminRequired("view all institutes");

        var result = await _instituteService.GetAllAsync();
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInstituteRequest request)
    {
        if (!User.IsSuperAdmin())
            return SuperAdminRequired("update institutes");

        var result = await _instituteService.UpdateAsync(id, request);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!User.IsSuperAdmin())
            return SuperAdminRequired("delete institutes");

        var result = await _instituteService.DeleteAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    private ObjectResult SuperAdminRequired(string action)
        => StatusCode(StatusCodes.Status403Forbidden,
            ApiResponse<object>.Fail($"Only SuperAdmin users can {action}.", "SUPER_ADMIN_REQUIRED"));
}
