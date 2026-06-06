using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Leave;

public class LeaveRequestDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int? LeaveTypeId { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? SupervisorUserId { get; set; }
    public string? SupervisorName { get; set; }
    public string? SupervisorLoginID { get; set; }
    public string? SupervisorDesignationName { get; set; }
    public int? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLeaveRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    public int? LeaveTypeId { get; set; }

    [Required]
    public string LeaveType { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class ApproveLeaveRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;

    public string? Comments { get; set; }
}

public class CancelLeaveRequest
{
    public string? Comments { get; set; }
}

public class LeaveTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
