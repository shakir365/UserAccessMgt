using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required, MinLength(3), MaxLength(50)]
    public string LoginID { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    [Required, RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "MobileNumber must be a valid BD mobile number.")]
    public string MobileNumber { get; set; } = string.Empty;

    [Required]
    public string InstituteCode { get; set; } = string.Empty;

    public int? GradeId { get; set; }
    public int? DesignationId { get; set; }
}
