using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Domain.Entities;

public class Department
{
    [Key]
    public int Id { get; set; }

    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
}