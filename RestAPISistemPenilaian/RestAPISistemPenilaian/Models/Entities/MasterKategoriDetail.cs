using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestAPISistemPenilaian.Models.Entities
{
    [Table("Master_Kategori_Detail")]
    public class MasterKategoriDetail
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("MasterKategori")]
        public int KategoriId { get; set; }

        [Required]
        [MaxLength(50)]
        public string NamaGrade { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal NilaiMin { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal NilaiMax { get; set; }

        [Required]
        [MaxLength(150)]
        public string Rekomendasi { get; set; } = string.Empty;

        public string? Keterangan { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation
        public MasterKategori? MasterKategori { get; set; }
    }
}
