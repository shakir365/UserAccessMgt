using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Department;

public class DepartmentDto
{
    public int Id { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreateDate { get; set; }
}

public class CreateDepartmentRequest
{
    [Required]
    public string DepartmentCode { get; set; } = string.Empty;

    [Required]
    public string DepartmentName { get; set; } = string.Empty;
}

public class UpdateDepartmentRequest
{
    public string? DepartmentName { get; set; }
    public bool? IsActive { get; set; }
}