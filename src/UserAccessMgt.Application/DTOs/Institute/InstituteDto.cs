using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Application.DTOs.Institute;

public class InstituteDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string InstituteNameEN { get; set; } = string.Empty;
    public string InstituteNameBN { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? LatitudeLongitude { get; set; }
}

public class PagedInstituteResult
{
    public IEnumerable<InstituteDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}

public class CreateInstituteRequest
{
    [Required, MinLength(1)]
    public string Code { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string InstituteNameEN { get; set; } = string.Empty;

    public string InstituteNameBN { get; set; } = string.Empty;

    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? LatitudeLongitude { get; set; }
}

public class UpdateInstituteRequest
{
    public string? InstituteNameEN { get; set; }
    public string? InstituteNameBN { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool? IsActive { get; set; }
    public string? LatitudeLongitude { get; set; }
}
