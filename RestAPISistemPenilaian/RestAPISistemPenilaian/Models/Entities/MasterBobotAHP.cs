using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestAPISistemPenilaian.Models.Entities
{
    [Table("Master_BobotAHP")]
    public class MasterBobotAHP
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string KodeAturan { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string NamaAturan { get; set; } = string.Empty;

        public string? Keterangan { get; set; }

        public ICollection<MasterBobotAHPKriteria> KriteriaDetails { get; set; } = new List<MasterBobotAHPKriteria>();

        [Column(TypeName = "decimal(6,4)")]
        public decimal Bobot_KerjasamaTim { get; set; } = 0.1500m;

        [Column(TypeName = "decimal(6,4)")]
        public decimal Bobot_KetepatanWaktu { get; set; } = 0.2000m;

        [Column(TypeName = "decimal(6,4)")]
        public decimal Bobot_TanggungJawab { get; set; } = 0.2500m;

        [Column(TypeName = "decimal(6,4)")]
        public decimal Bobot_Inisiatif { get; set; } = 0.1000m;

        [Column(TypeName = "decimal(6,4)")]
        public decimal Bobot_KeterlibatanKerja { get; set; } = 0.1000m;

        [Column(TypeName = "decimal(6,4)")]
        public decimal Bobot_InteraksiKolega { get; set; } = 0.0500m;

        [Column(TypeName = "decimal(6,4)")]
        public decimal Bobot_TindakLanjut { get; set; } = 0.1500m;

        [Column(TypeName = "decimal(8,4)")]
        public decimal ConsistencyRatio_CR { get; set; } = 0.0000m;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
