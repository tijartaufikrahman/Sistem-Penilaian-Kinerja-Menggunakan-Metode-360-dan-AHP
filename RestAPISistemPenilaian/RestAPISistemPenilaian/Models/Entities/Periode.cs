using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestAPISistemPenilaian.Models.Entities
{
    [Table("Periode")]
    public class Periode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string KodePeriode { get; set; } = string.Empty;

        [ForeignKey("MasterBobotAHP")]
        public int BobotAHPId { get; set; }

        [ForeignKey("MasterKategori")]
        public int KategoriId { get; set; }

        [Required]
        [MaxLength(100)]
        public string NamaPeriode { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Draft"; // Draft, Berjalan, Selesai

        public DateTime TanggalMulai { get; set; }

        public DateTime TanggalSelesai { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation
        public MasterBobotAHP? MasterBobotAHP { get; set; }
        public MasterKategori? MasterKategori { get; set; }
        public ICollection<HasilPenilaianSistem> HasilPenilaianSistems { get; set; } = new List<HasilPenilaianSistem>();
    }
}
