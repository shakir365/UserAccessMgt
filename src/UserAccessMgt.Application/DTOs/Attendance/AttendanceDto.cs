using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Attendance;

public class AttendanceDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public int SubmittedByUserId { get; set; }
    public string SubmittedByUserName { get; set; } = string.Empty;
}

public class CreateAttendanceRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public DateTime? CheckInTime { get; set; }
    [Range(-90, 90)]
    public double? CheckInLatitude { get; set; }
    [Range(-180, 180)]
    public double? CheckInLongitude { get; set; }
    public DateTime? CheckOutTime { get; set; }
    [Range(-90, 90)]
    public double? CheckOutLatitude { get; set; }
    [Range(-180, 180)]
    public double? CheckOutLongitude { get; set; }

    [Required]
    public string Status { get; set; } = "Present";

    public string? Notes { get; set; }

    [Range(1, int.MaxValue)]
    public int InstituteId { get; set; }
}

public class UpdateAttendanceRequest
{
    public DateTime? CheckInTime { get; set; }
    [Range(-90, 90)]
    public double? CheckInLatitude { get; set; }
    [Range(-180, 180)]
    public double? CheckInLongitude { get; set; }
    public DateTime? CheckOutTime { get; set; }
    [Range(-90, 90)]
    public double? CheckOutLatitude { get; set; }
    [Range(-180, 180)]
    public double? CheckOutLongitude { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}
