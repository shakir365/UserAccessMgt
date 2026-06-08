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

public class AttendanceAnalyticalReportRequest
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Period { get; set; }
    public int? DivisionId { get; set; }
    public int? DistrictId { get; set; }
    public int? ThanaId { get; set; }
    public int? InstituteId { get; set; }
    public int? UserId { get; set; }
}

public class AttendanceAnalyticalReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string Period { get; set; } = "Day";
    public AttendanceAnalyticalSummaryDto Summary { get; set; } = new();
    public AttendanceAnalyticalFilterOptionsDto FilterOptions { get; set; } = new();
    public IEnumerable<AttendanceAnalyticalReportRowDto> Rows { get; set; } = [];
}

public class AttendanceAnalyticalSummaryDto
{
    public int InstituteCount { get; set; }
    public int TeacherCount { get; set; }
    public int ExpectedCount { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int LeaveCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal PresentRate { get; set; }
    public decimal LateRate { get; set; }
    public decimal LeaveRate { get; set; }
}

public class AttendanceAnalyticalReportRowDto : AttendanceAnalyticalSummaryDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public int InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public int? ThanaId { get; set; }
    public string? ThanaName { get; set; }
    public int? DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public int? DivisionId { get; set; }
    public string? DivisionName { get; set; }
}

public class AttendanceAnalyticalFilterOptionsDto
{
    public IEnumerable<AttendanceAnalyticalFilterOptionDto> Divisions { get; set; } = [];
    public IEnumerable<AttendanceAnalyticalFilterOptionDto> Districts { get; set; } = [];
    public IEnumerable<AttendanceAnalyticalFilterOptionDto> Thanas { get; set; } = [];
    public IEnumerable<AttendanceAnalyticalFilterOptionDto> Institutes { get; set; } = [];
    public IEnumerable<AttendanceAnalyticalFilterOptionDto> Users { get; set; } = [];
}

public class AttendanceAnalyticalFilterOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
}

public class AttendancePersonalAnalyticalReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string Period { get; set; } = "Day";
    public AttendanceAnalyticalSummaryDto Summary { get; set; } = new();
    public AttendanceAnalyticalFilterOptionsDto FilterOptions { get; set; } = new();
    public IEnumerable<AttendancePersonalAnalyticalReportRowDto> Rows { get; set; } = [];
}

public class AttendancePersonalAnalyticalReportRowDto : AttendanceAnalyticalSummaryDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? LoginId { get; set; }
    public string? DesignationName { get; set; }
    public int InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public int? ThanaId { get; set; }
    public string? ThanaName { get; set; }
    public int? DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public int? DivisionId { get; set; }
    public string? DivisionName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? CheckInLatitudeLongitude { get; set; }
    public string? CheckOutLatitudeLongitude { get; set; }
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
