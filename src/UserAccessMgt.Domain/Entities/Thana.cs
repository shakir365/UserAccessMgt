using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserAccessMgt.Domain.Entities
{
    [Table("Thanas")]
    public class Thana
    {
        [Key]
        public int ThanaId { get; set; }

        [Required, MaxLength(100)]
        public string ThanaNameEN { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string ThanaNameBN { get; set; } = string.Empty;

        [ForeignKey(nameof(District))]
        public int DistrictId { get; set; }
        public District District { get; set; } = null!;

        public ICollection<Institute> Institutes { get; set; } = new List<Institute>();
    }
}
