using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Domain.Entities;

public class Grade
{
    [Key]
    public int Id { get; set; }

    public string GradeCode { get; set; } = string.Empty;
    public string GradeName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
}