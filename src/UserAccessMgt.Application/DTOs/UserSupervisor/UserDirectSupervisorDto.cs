using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.UserSupervisor;

public class UserDirectSupervisorDto
{
    public int Id { get; set; }
    public int UserID { get; set; }
    public string UserLoginID { get; set; } = string.Empty;
    public int Supervisor_UserID { get; set; }
    public string SupervisorLoginID { get; set; } = string.Empty;
    public DateTime ActiveDateFrom { get; set; }
    public DateTime? ExpireDate { get; set; }
    public DateTime? CreateDate { get; set; }
    public int? CreateBy_UserID { get; set; }
    public string? CreateByLoginID { get; set; }
}

public class UserSupervisorSetRequest
{
    [Range(1, int.MaxValue)]
    public int UserID { get; set; }

    [Range(1, int.MaxValue)]
    public int Supervisor_UserID { get; set; }

    [Required]
    public DateTime ActiveDateFrom { get; set; }

    public DateTime? ExpireDate { get; set; }
}

public class UserDirectSupervisorLookupDto
{
    public UserDirectSupervisorDto Configuration { get; set; } = new();
    public UserAccessMgt.Application.DTOs.User.UserDto Supervisor { get; set; } = new();
}
