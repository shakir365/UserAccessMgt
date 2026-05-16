using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserAccessMgt.Domain.Entities
{
    [Table("Districts")]
    public class District
    {
        [Key]
        public int DistrictId { get; set; }

        [Required, MaxLength(100)]
        public string DistrictNameEN { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string DistrictNameBN { get; set; } = string.Empty;


        [ForeignKey(nameof(Division))]
        public int DivisionId { get; set; }
        public Division Division { get; set; } = null!;

        public ICollection<Thana> Thanas { get; set; } = new List<Thana>();
    }
}
