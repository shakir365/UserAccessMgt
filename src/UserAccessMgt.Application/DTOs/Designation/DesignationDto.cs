using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Designation;

public class DesignationDto
{
    public int Id { get; set; }
    public string DesignationCode { get; set; } = string.Empty;
    public string DesignationName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreateDate { get; set; }
}

public class CreateDesignationRequest
{
    [Required]
    public string DesignationCode { get; set; } = string.Empty;

    [Required]
    public string DesignationName { get; set; } = string.Empty;
}

public class UpdateDesignationRequest
{
    public string? DesignationName { get; set; }
    public bool? IsActive { get; set; }
}