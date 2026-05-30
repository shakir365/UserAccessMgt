using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Holiday;

public class HolidayDto
{
    public int Id { get; set; }
    public string HolidayName { get; set; } = string.Empty;
    public DateTime HolidayDate { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateHolidayRequest
{
    [Required, MinLength(1)]
    public string HolidayName { get; set; } = string.Empty;

    [Required]
    public DateTime HolidayDate { get; set; }

    public string? Description { get; set; }
}

public class UpdateHolidayRequest
{
    public string? HolidayName { get; set; }
    public DateTime? HolidayDate { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
