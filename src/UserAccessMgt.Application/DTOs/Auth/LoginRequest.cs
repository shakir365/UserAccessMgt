using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Auth;

public class LoginRequest
{
    [Required]
    public string LoginID { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
