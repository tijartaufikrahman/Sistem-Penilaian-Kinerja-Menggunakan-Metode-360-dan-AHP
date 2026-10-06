using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestAPISistemPenilaian.Models.Entities
{
    [Table("Master_Kriteria")]
    public class MasterKriteria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string KodeKriteria { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string NamaKriteria { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Deskripsi { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        public ICollection<MasterBobotAHPKriteria> BobotAHPDetails { get; set; } = new List<MasterBobotAHPKriteria>();
    }
}
