using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAccessMgt.Api.Authorization;
using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Designation;
using UserAccessMgt.Application.Interfaces;

namespace UserAccessMgt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DesignationController : ControllerBase
{
    private readonly IDesignationService _designationService;

    public DesignationController(IDesignationService designationService)
    {
        _designationService = designationService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDesignationRequest request)
    {
        if (!User.IsSuperAdmin())
            return SuperAdminRequired("create designations");

        var result = await _designationService.CreateAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _designationService.GetByIdAsync(id);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("code/{designationCode}")]
    public async Task<IActionResult> GetByCode(string designationCode)
    {
        var result = await _designationService.GetByCodeAsync(designationCode);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _designationService.GetAllAsync();
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDesignationRequest request)
    {
        if (!User.IsSuperAdmin())
            return SuperAdminRequired("update designations");

        var result = await _designationService.UpdateAsync(id, request);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!User.IsSuperAdmin())
            return SuperAdminRequired("delete designations");

        var result = await _designationService.DeleteAsync(id);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    private ObjectResult SuperAdminRequired(string action)
        => StatusCode(StatusCodes.Status403Forbidden,
            ApiResponse<object>.Fail($"Only SuperAdmin users can {action}.", "SUPER_ADMIN_REQUIRED"));
}
