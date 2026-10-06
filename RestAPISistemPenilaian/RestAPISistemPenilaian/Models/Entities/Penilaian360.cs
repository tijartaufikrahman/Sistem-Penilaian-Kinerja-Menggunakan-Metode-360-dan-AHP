using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestAPISistemPenilaian.Models.Entities
{
    [Table("Penilaian360")]
    public class Penilaian360
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string KodePenilaian { get; set; } = string.Empty;

        [ForeignKey("Periode")]
        public int? PeriodeId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; } // Karyawan yang dinilai

        public int PenilaiUserId { get; set; } // Karyawan yang menilai

        [Required]
        [MaxLength(20)]
        public string TipePenilai { get; set; } = "Rekan"; // Self, Atasan, Rekan

        [Column(TypeName = "decimal(5,2)")]
        public decimal BobotPersen { get; set; } = 30.00m; // Default persen penilai

        // 7 Parameter Kriteria Nilai (1 - 100)
        [Column(TypeName = "decimal(5,2)")]
        public decimal kerjasama_tim { get; set; } = 0.00m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal ketepatan_waktu { get; set; } = 0.00m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal tanggung_jawab { get; set; } = 0.00m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal inisiatif { get; set; } = 0.00m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal keterlibatan_kerja { get; set; } = 0.00m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal interaksi_kolega { get; set; } = 0.00m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal tindak_lanjut { get; set; } = 0.00m;

        public DateTime TanggalPenilaian { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(20)]
        public string StatusPengisian { get; set; } = "Draft"; // Draft, Selesai

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation
        public User? User { get; set; }
        public Periode? Periode { get; set; }
    }
}
