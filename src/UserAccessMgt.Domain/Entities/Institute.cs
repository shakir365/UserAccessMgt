namespace UserAccessMgt.Domain.Entities;

public class Institute
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<UserTransfer> FromInstituteTransfers { get; set; } = new List<UserTransfer>();
    public ICollection<UserTransfer> ToInstituteTransfers { get; set; } = new List<UserTransfer>();
}
