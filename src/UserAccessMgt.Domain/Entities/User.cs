namespace UserAccessMgt.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string LoginID { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string MobileNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public int InstituteId { get; set; }
    public Institute Institute { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<LoginHistory> LoginHistories { get; set; } = new List<LoginHistory>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<Attendance> SubmittedAttendances { get; set; } = new List<Attendance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<LeaveRequest> ApprovedLeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<UserTransfer> UserTransfers { get; set; } = new List<UserTransfer>();
    public ICollection<UserTransfer> TransferredByRecords { get; set; } = new List<UserTransfer>();
}
