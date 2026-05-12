namespace UserAccessMgt.Domain.Entities;

public class Attendance
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public string Status { get; set; } = "Present";
    public string? Notes { get; set; }

    public int InstituteId { get; set; }
    public Institute Institute { get; set; } = null!;

    public int SubmittedByUserId { get; set; }
    public User SubmittedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
