using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Domain.Entities;

public class Designation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string DesignationCode { get; set; } = string.Empty;
    [Required]
    public string DesignationNameEN { get; set; } = string.Empty;
    public string DesignationNameBN { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public ICollection<User> Users { get; set; } = new List<User>();
}
