using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
