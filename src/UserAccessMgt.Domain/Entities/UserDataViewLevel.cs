using System.ComponentModel.DataAnnotations;

namespace UserAccessMgt.Domain.Entities;

public class UserDataViewLevel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string DataViewLevel { get; set; } = string.Empty;

    [Required]
    public string RelatedRoleInfo { get; set; } = string.Empty;

    public ICollection<Role> Roles { get; set; } = new List<Role>();
}
