namespace UserAccessMgt.Domain.Entities;

public class UserDirectSupervisor
{
    public int Id { get; set; }

    public int UserID { get; set; }
    public User User { get; set; } = null!;

    public int Supervisor_UserID { get; set; }
    public User SupervisorUser { get; set; } = null!;

    public DateTime ActiveDateFrom { get; set; }
    public DateTime? ExpireDate { get; set; }
    public DateTime? CreateDate { get; set; }

    public int? CreateBy_UserID { get; set; }
    public User? CreateByUser { get; set; }
}
