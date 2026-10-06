using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestAPISistemPenilaian.Models.Entities
{
    [Table("Master_BobotAHP_Kriteria")]
    public class MasterBobotAHPKriteria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("MasterBobotAHP")]
        public int BobotAHPId { get; set; }

        public MasterBobotAHP? MasterBobotAHP { get; set; }

        [Required]
        [ForeignKey("MasterKriteria")]
        public int KriteriaId { get; set; }

        public MasterKriteria? MasterKriteria { get; set; }

        [Column(TypeName = "decimal(6,4)")]
        public decimal Bobot { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
    }
}
