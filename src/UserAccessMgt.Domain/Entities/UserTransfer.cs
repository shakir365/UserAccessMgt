namespace UserAccessMgt.Domain.Entities;

public class UserTransfer
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int FromInstituteId { get; set; }
    public Institute FromInstitute { get; set; } = null!;
    public int ToInstituteId { get; set; }
    public Institute ToInstitute { get; set; } = null!;
    public int TransferredById { get; set; }
    public User TransferredBy { get; set; } = null!;
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
