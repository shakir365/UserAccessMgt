using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Grade;

public class GradeDto
{
    public int Id { get; set; }
    public string GradeCode { get; set; } = string.Empty;
    public string GradeName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreateDate { get; set; }
}

public class CreateGradeRequest
{
    [Required]
    public string GradeCode { get; set; } = string.Empty;

    [Required]
    public string GradeName { get; set; } = string.Empty;
}

public class UpdateGradeRequest
{
    public string? GradeName { get; set; }
    public bool? IsActive { get; set; }
}