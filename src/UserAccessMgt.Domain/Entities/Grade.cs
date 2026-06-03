using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Domain.Entities;

public class Grade
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string GradeCode { get; set; } = string.Empty;
    [Required]
    public string GradeNameEN { get; set; } = string.Empty;
    public string GradeNameBN { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public ICollection<User> Users { get; set; } = new List<User>();
}
