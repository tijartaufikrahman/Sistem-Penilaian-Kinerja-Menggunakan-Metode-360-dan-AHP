using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestAPISistemPenilaian.Models.Entities
{
    [Table("Master_Kategori")]
    public class MasterKategori
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string KodeKategori { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string NamaKategori { get; set; } = string.Empty;

        public string? Deskripsi { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation
        public ICollection<MasterKategoriDetail> Details { get; set; } = new List<MasterKategoriDetail>();
    }
}
