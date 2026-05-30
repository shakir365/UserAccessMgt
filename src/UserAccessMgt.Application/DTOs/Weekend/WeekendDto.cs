using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Weekend;

public class WeekendDto
{
    public int Id { get; set; }
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateWeekendRequest
{
    [Range(0, 6)]
    public int DayOfWeek { get; set; }
}

public class UpdateWeekendRequest
{
    [Range(0, 6)]
    public int? DayOfWeek { get; set; }
    public bool? IsActive { get; set; }
}
