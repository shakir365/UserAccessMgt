namespace UserAccessMgt.Domain.Entities;

public class Weekend
{
    public int Id { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
