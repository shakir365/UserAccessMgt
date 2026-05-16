using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserAccessMgt.Domain.Entities
{
    [Table("Divisions")]
    public class Division
    {
        [Key]
        public int DivisionId { get; set; }

        [Required, MaxLength(100)]
        public string DivisionNameEN { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string DivisionNameBN { get; set; } = string.Empty;

        public ICollection<District> Districts { get; set; } = new List<District>();
    }
}
