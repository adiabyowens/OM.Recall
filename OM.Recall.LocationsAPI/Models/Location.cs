using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OM.Recall.LocationsAPI.Models
{
    [Table("Locations")]
    public class Location
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Identifier { get; set; } = string.Empty;

        [StringLength(100)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string SystemTypeName { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
