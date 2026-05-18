using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Designation;

public class DesignationDto
{
    public int Id { get; set; }
    public string DesignationCode { get; set; } = string.Empty;
    public string DesignationNameEN { get; set; } = string.Empty;
    public string DesignationNameBN { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreateDate { get; set; }
}

public class CreateDesignationRequest
{
    [Required, MinLength(1)]
    public string DesignationCode { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string DesignationNameEN { get; set; } = string.Empty;

    public string DesignationNameBN { get; set; } = string.Empty;
}

public class UpdateDesignationRequest
{
    public string? DesignationNameEN { get; set; }
    public string? DesignationNameBN { get; set; }
    public bool? IsActive { get; set; }
}
