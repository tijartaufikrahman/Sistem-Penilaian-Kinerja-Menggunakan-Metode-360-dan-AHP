using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestAPISistemPenilaian.Models.Entities
{
    [Table("HasilPenilaianSistem")]
    public class HasilPenilaianSistem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string KodeHasil { get; set; } = string.Empty;

        [ForeignKey("Periode")]
        public int PeriodeId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [ForeignKey("MasterKategoriDetail")]
        public int KategoriDetailId { get; set; }

        // Rata-rata Gabungan 360 Derajat untuk 7 Kriteria
        [Column(TypeName = "decimal(5,2)")]
        public decimal Rata2_KerjasamaTim { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Rata2_KetepatanWaktu { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Rata2_TanggungJawab { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Rata2_Inisiatif { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Rata2_KeterlibatanKerja { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Rata2_InteraksiKolega { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Rata2_TindakLanjut { get; set; }

        // Output Kalkulasi AHP
        [Column(TypeName = "decimal(5,2)")]
        public decimal SkorAkhirAHP { get; set; }

        public int Ranking { get; set; }

        public string? CatatanHRD { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation
        public Periode? Periode { get; set; }
        public User? User { get; set; }
        public MasterKategoriDetail? MasterKategoriDetail { get; set; }
    }
}
