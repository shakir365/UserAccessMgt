using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Shift;

public class ShiftDto
{
    public int Id { get; set; }
    public string ShiftCode { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int LateAfterMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateShiftRequest
{
    [Required, MinLength(1)]
    public string ShiftCode { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string ShiftName { get; set; } = string.Empty;

    [Required]
    public string StartTime { get; set; } = string.Empty;

    [Required]
    public string EndTime { get; set; } = string.Empty;

    [Range(0, 1440)]
    public int LateAfterMinutes { get; set; }
}

public class UpdateShiftRequest
{
    public string? ShiftName { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }

    [Range(0, 1440)]
    public int? LateAfterMinutes { get; set; }
    public bool? IsActive { get; set; }
}
