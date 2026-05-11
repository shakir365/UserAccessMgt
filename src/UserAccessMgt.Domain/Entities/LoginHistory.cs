namespace UserAccessMgt.Domain.Entities;

public class LoginHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime LoginAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
}
