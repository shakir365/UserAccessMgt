namespace UserAccessMgt.Application.DTOs.User;

public class UserDto
{
    public int Id { get; set; }
    public string LoginID { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string MobileNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int InstituteId { get; set; }
    public string InstituteName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    [System.ComponentModel.DataAnnotations.RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "MobileNumber must be a valid BD mobile number.")]
    public string? MobileNumber { get; set; }
    public bool? IsActive { get; set; }
}
