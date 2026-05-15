using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Domain.Entities;

public class Designation
{
    [Key]
    public int Id { get; set; }

    public string DesignationCode { get; set; } = string.Empty;
    public string DesignationName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
}