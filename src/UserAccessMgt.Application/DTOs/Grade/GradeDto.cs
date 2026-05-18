using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Grade;

public class GradeDto
{
    public int Id { get; set; }
    public string GradeCode { get; set; } = string.Empty;
    public string GradeNameEN { get; set; } = string.Empty;
    public string GradeNameBN { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreateDate { get; set; }
}

public class CreateGradeRequest
{
    [Required, MinLength(1)]
    public string GradeCode { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string GradeNameEN { get; set; } = string.Empty;

    public string GradeNameBN { get; set; } = string.Empty;
}

public class UpdateGradeRequest
{
    public string? GradeNameEN { get; set; }
    public string? GradeNameBN { get; set; }
    public bool? IsActive { get; set; }
}
