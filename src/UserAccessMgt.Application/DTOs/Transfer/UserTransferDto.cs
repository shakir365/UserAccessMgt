using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Transfer;

public class UserTransferDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FromInstituteName { get; set; } = string.Empty;
    public string ToInstituteName { get; set; } = string.Empty;
    public string TransferredByName { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public string? Reason { get; set; }
}

public class CreateTransferRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int ToInstituteId { get; set; }

    public string? Reason { get; set; }
}
