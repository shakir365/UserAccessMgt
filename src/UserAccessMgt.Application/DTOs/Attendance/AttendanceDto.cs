using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Attendance;

public class AttendanceDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? CheckInLatitudeLongitude { get; set; }
    public string? CheckOutLatitudeLongitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public int SubmittedByUserId { get; set; }
    public string SubmittedByUserName { get; set; } = string.Empty;
}

public class AttendanceSubmissionStatusDto
{
    public DateTime Date { get; set; }
    public bool IsAllowed { get; set; }
    public string? ReasonCode { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CreateAttendanceRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    public DateTime? Date { get; set; }

    public DateTime? CheckInTime { get; set; }
    public string? CheckInLatitudeLongitude { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? CheckOutLatitudeLongitude { get; set; }

    public string? Status { get; set; }

    public string? Notes { get; set; }

    [Range(1, int.MaxValue)]
    public int InstituteId { get; set; }
}

public class UpdateAttendanceRequest
{
    public DateTime? CheckInTime { get; set; }
    public string? CheckInLatitudeLongitude { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? CheckOutLatitudeLongitude { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}
