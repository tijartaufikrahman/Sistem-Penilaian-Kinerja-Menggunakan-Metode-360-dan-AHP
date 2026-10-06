namespace RestAPISistemPenilaian.Models.DTOs
{
    // ==========================================
    // 0. MASTER KRITERIA DTOs
    // ==========================================
    public class MasterKriteriaDto
    {
        public int Id { get; set; }
        public string KodeKriteria { get; set; } = string.Empty;
        public string NamaKriteria { get; set; } = string.Empty;
        public string? Deskripsi { get; set; }
        public bool IsActive { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class SaveMasterKriteriaDto
    {
        public int? Id { get; set; }
        public string KodeKriteria { get; set; } = string.Empty;
        public string NamaKriteria { get; set; } = string.Empty;
        public string? Deskripsi { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class BobotKriteriaDetailDto
    {
        public int KriteriaId { get; set; }
        public string KodeKriteria { get; set; } = string.Empty;
        public string NamaKriteria { get; set; } = string.Empty;
        public decimal Bobot { get; set; }
    }

    // ==========================================
    // 1. MASTER BOBOT AHP DTOs
    // ==========================================
    public class MasterBobotAHPDto
    {
        public int Id { get; set; }
        public string KodeAturan { get; set; } = string.Empty;
        public string NamaAturan { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
        public decimal Bobot_KerjasamaTim { get; set; }
        public decimal Bobot_KetepatanWaktu { get; set; }
        public decimal Bobot_TanggungJawab { get; set; }
        public decimal Bobot_Inisiatif { get; set; }
        public decimal Bobot_KeterlibatanKerja { get; set; }
        public decimal Bobot_InteraksiKolega { get; set; }
        public decimal Bobot_TindakLanjut { get; set; }
        public decimal ConsistencyRatio_CR { get; set; }
        public bool IsActive { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public List<BobotKriteriaDetailDto> KriteriaDetails { get; set; } = new();
    }

    public class UpdateBobotStatusDto
    {
        public bool IsActive { get; set; }
    }

    public class CreateMasterBobotAHPDto
    {
        public string KodeAturan { get; set; } = string.Empty;
        public string NamaAturan { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
        public decimal Bobot_KerjasamaTim { get; set; }
        public decimal Bobot_KetepatanWaktu { get; set; }
        public decimal Bobot_TanggungJawab { get; set; }
        public decimal Bobot_Inisiatif { get; set; }
        public decimal Bobot_KeterlibatanKerja { get; set; }
        public decimal Bobot_InteraksiKolega { get; set; }
        public decimal Bobot_TindakLanjut { get; set; }
        public decimal ConsistencyRatio_CR { get; set; }
        public bool IsActive { get; set; } = true;
        public List<BobotKriteriaDetailDto>? KriteriaBobot { get; set; }
    }

    // Pairwise Comparison Input: 21 pasang perbandingan (kriteria 1-7)
    public class AhpPairwiseRequestDto
    {
        public string? KodeAturan { get; set; }
        public string? NamaAturan { get; set; }
        // Matrix 7x7 input or 21 pairs
        // Nilai perbandingan antar kriteria (baris i, kolom j)
        // c1: Kerjasama, c2: Waktu, c3: Tanggung Jawab, c4: Inisiatif, c5: Keterlibatan, c6: Interaksi, c7: Tindak Lanjut
        public double[][]? Matrix { get; set; }
        public bool SaveIfConsistent { get; set; } = false;
    }

    public class AhpCalculationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double[][]? NormalizedMatrix { get; set; }
        public double[]? Weights { get; set; }
        public double LambdaMax { get; set; }
        public double ConsistencyIndex_CI { get; set; }
        public double ConsistencyRatio_CR { get; set; }
        public bool IsConsistent { get; set; } // CR <= 0.1
        public MasterBobotAHPDto? SavedRule { get; set; }
    }

    // ==========================================
    // 2. MASTER KATEGORI & DETAIL DTOs (HEADER - DETAIL)
    // ==========================================
    public class MasterKategoriDetailDto
    {
        public int Id { get; set; }
        public int KategoriId { get; set; }
        public string NamaGrade { get; set; } = string.Empty;
        public decimal NilaiMin { get; set; }
        public decimal NilaiMax { get; set; }
        public string Rekomendasi { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class MasterKategoriDto
    {
        public int Id { get; set; }
        public string KodeKategori { get; set; } = string.Empty;
        public string NamaKategori { get; set; } = string.Empty;
        public string? Deskripsi { get; set; }
        public bool IsActive { get; set; } = true;
        public int TotalDetail { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public List<MasterKategoriDetailDto> Details { get; set; } = new List<MasterKategoriDetailDto>();
    }

    public class SaveMasterKategoriDto
    {
        public int? Id { get; set; }
        public string KodeKategori { get; set; } = string.Empty;
        public string NamaKategori { get; set; } = string.Empty;
        public string? Deskripsi { get; set; }
        public bool IsActive { get; set; } = true;
        public List<SaveMasterKategoriDetailDto>? Details { get; set; }
    }

    public class SaveMasterKategoriDetailDto
    {
        public int? Id { get; set; }
        public int KategoriId { get; set; }
        public string NamaGrade { get; set; } = string.Empty;
        public decimal NilaiMin { get; set; }
        public decimal NilaiMax { get; set; }
        public string Rekomendasi { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
    }

    // ==========================================
    // 3. PERIODE PENILAIAN DTOs
    // ==========================================
    public class PeriodeDto
    {
        public int Id { get; set; }
        public string KodePeriode { get; set; } = string.Empty;
        public int BobotAHPId { get; set; }
        public string NamaAturanAHP { get; set; } = string.Empty;
        public MasterBobotAHPDto? BobotAHP { get; set; }
        public int KategoriId { get; set; }
        public string NamaKategori { get; set; } = string.Empty;
        public string NamaPeriode { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft"; // Draft, Berjalan, Selesai
        public string TanggalMulai { get; set; } = string.Empty;
        public string TanggalSelesai { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public int TotalPeserta { get; set; }
        public int TotalEvaluasiSelesai { get; set; }
        public int TotalAkunMengisi { get; set; }
        public int TotalAkunPenilai { get; set; }
        public int TotalTugasSelesai { get; set; }
        public int TotalTugas { get; set; }
        public int TotalKaryawanDinilai { get; set; }
        public int TotalKaryawanSelesai { get; set; }
        public double PersentasePengisian { get; set; }
    }

    public class CreatePeriodeDto
    {
        public string KodePeriode { get; set; } = string.Empty;
        public int BobotAHPId { get; set; }
        public int KategoriId { get; set; }
        public string NamaPeriode { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft";
        public DateTime TanggalMulai { get; set; }
        public DateTime TanggalSelesai { get; set; }

        // Parameter Partisipan & Relasi Penilai (Nomor 3)
        public string? SkemaPenilai { get; set; } = "ALL_360"; // "ALL_360", "HRD_ONLY", "ROLE_BASED", "CUSTOM_USER"
        public int? TargetRoleId { get; set; }
        public List<int>? TargetUserIds { get; set; }
        public bool IncludePeerReview { get; set; } = true;
        public bool IncludeSupervisorReview { get; set; } = true;
        public bool ExcludeHrdFromTarget { get; set; } = true;
    }

    // ==========================================
    // 4. PENILAIAN 360 DTOs
    // ==========================================
    public class Penilaian360Dto
    {
        public int Id { get; set; }
        public string KodePenilaian { get; set; } = string.Empty;
        public int? PeriodeId { get; set; }
        public string NamaPeriode { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string NamaKaryawanDinilai { get; set; } = string.Empty;
        public string KodeKaryawanDinilai { get; set; } = string.Empty;
        public int PenilaiUserId { get; set; }
        public string NamaPenilai { get; set; } = string.Empty;
        public string TipePenilai { get; set; } = string.Empty; // Self, Atasan, Rekan
        public decimal BobotPersen { get; set; }
        public decimal kerjasama_tim { get; set; }
        public decimal ketepatan_waktu { get; set; }
        public decimal tanggung_jawab { get; set; }
        public decimal inisiatif { get; set; }
        public decimal keterlibatan_kerja { get; set; }
        public decimal interaksi_kolega { get; set; }
        public decimal tindak_lanjut { get; set; }
        public string TanggalPenilaian { get; set; } = string.Empty;
        public string StatusPengisian { get; set; } = string.Empty;
    }

    public class SavePenilaian360Dto
    {
        public int? Id { get; set; }
        public string? KodePenilaian { get; set; }
        public int? PeriodeId { get; set; }
        public int UserId { get; set; } // Karyawan yang dinilai
        public int PenilaiUserId { get; set; } // Karyawan penilai
        public string TipePenilai { get; set; } = "Rekan";
        public decimal BobotPersen { get; set; } = 30.00m;
        public decimal kerjasama_tim { get; set; }
        public decimal ketepatan_waktu { get; set; }
        public decimal tanggung_jawab { get; set; }
        public decimal inisiatif { get; set; }
        public decimal keterlibatan_kerja { get; set; }
        public decimal interaksi_kolega { get; set; }
        public decimal tindak_lanjut { get; set; }
        public string StatusPengisian { get; set; } = "Selesai";
    }

    public class MonitoringProgresPeriodeDto
    {
        public int PeriodeId { get; set; }
        public string NamaPeriode { get; set; } = string.Empty;
        public string StatusPeriode { get; set; } = string.Empty;
        public int TotalTugasEvaluasi { get; set; }
        public int TotalSelesai { get; set; }
        public int TotalBelumSelesai { get; set; }
        public double PersentaseSelesai { get; set; }
        public bool SiapDihitung { get; set; }
        public int TotalAkunPenilai { get; set; }
        public int TotalAkunMengisi { get; set; }
        public int TotalAkunTuntas { get; set; }
    }

    // ==========================================
    // 5. HASIL PENILAIAN SISTEM DTOs
    // ==========================================
    public class HasilPenilaianSistemDto
    {
        public int Id { get; set; }
        public string KodeHasil { get; set; } = string.Empty;
        public int PeriodeId { get; set; }
        public string NamaPeriode { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string KodeKaryawan { get; set; } = string.Empty;
        public string NamaKaryawan { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int KategoriDetailId { get; set; }
        public int KategoriId { get; set; }
        public string NamaKategori { get; set; } = string.Empty;
        public string Rekomendasi { get; set; } = string.Empty;

        // Rata-rata 7 Kriteria
        public decimal Rata2_KerjasamaTim { get; set; }
        public decimal Rata2_KetepatanWaktu { get; set; }
        public decimal Rata2_TanggungJawab { get; set; }
        public decimal Rata2_Inisiatif { get; set; }
        public decimal Rata2_KeterlibatanKerja { get; set; }
        public decimal Rata2_InteraksiKolega { get; set; }
        public decimal Rata2_TindakLanjut { get; set; }

        public decimal SkorAkhirAHP { get; set; }
        public int Ranking { get; set; }
        public string? CatatanHRD { get; set; }
        public string CreatedAt { get; set; } = string.Empty;

        // Status Progres Penilaian Karyawan ini
        public int TotalEvaluasiMasuk { get; set; }
        public int TotalEvaluasiDiharapkan { get; set; }
        public bool SudahAdaPenilaian { get; set; }

        // Rincian kriteria dinamis yang dihitung pada aturan AHP
        public List<KriteriaNilaiItemDto> KriteriaNilai { get; set; } = new();
    }

    public class KriteriaNilaiItemDto
    {
        public int KriteriaId { get; set; }
        public string KodeKriteria { get; set; } = string.Empty;
        public string NamaKriteria { get; set; } = string.Empty;
        public decimal NilaiRataRata { get; set; }
        public decimal Bobot { get; set; }
        public decimal NilaiTertimbang { get; set; }
    }

    public class CalculateHasilPeriodeRequestDto
    {
        public int PeriodeId { get; set; }
    }
}
