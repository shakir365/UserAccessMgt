using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Domain.Entities;

public class Institute
{
    public int Id { get; set; }
    [Required]
    public string Code { get; set; } = string.Empty;
    [Required]
    public string InstituteNameEN { get; set; } = string.Empty;
    public string InstituteNameBN { get; set; } = string.Empty;
    public string? Address { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? LatitudeLongitude { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<UserTransfer> FromInstituteTransfers { get; set; } = new List<UserTransfer>();
    public ICollection<UserTransfer> ToInstituteTransfers { get; set; } = new List<UserTransfer>();
}
