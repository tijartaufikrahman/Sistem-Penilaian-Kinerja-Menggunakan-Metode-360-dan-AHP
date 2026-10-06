using RestAPISistemPenilaian.Models.DTOs;
using RestAPISistemPenilaian.Models.Entities;

namespace RestAPISistemPenilaian.Interfaces
{
    public interface ISistemPenilaianService
    {
        // Master Kriteria
        Task<List<MasterKriteriaDto>> GetAllKriteriaAsync();
        Task<MasterKriteriaDto?> GetKriteriaByIdAsync(int id);
        Task<MasterKriteriaDto> SaveKriteriaAsync(SaveMasterKriteriaDto dto, int? userId);
        Task<bool> DeleteKriteriaAsync(int id);

        // AHP
        Task<AhpCalculationResultDto> CalculateAhpAsync(AhpPairwiseRequestDto request, int? userId);
        Task<List<MasterBobotAHPDto>> GetAllBobotAhpAsync();
        Task<MasterBobotAHPDto?> GetBobotAhpByIdAsync(int id);
        Task<MasterBobotAHPDto?> GetActiveBobotAhpAsync();
        Task<MasterBobotAHPDto> CreateBobotAhpAsync(CreateMasterBobotAHPDto dto, int? userId);
        Task<bool> SetActiveBobotAhpAsync(int id, int? userId);
        Task<bool> UpdateStatusBobotAhpAsync(int id, bool isActive, int? userId);
        Task<bool> DeleteBobotAhpAsync(int id);

        // Kategori (Header & Detail)
        Task<List<MasterKategoriDto>> GetAllKategoriAsync();
        Task<MasterKategoriDto?> GetKategoriByIdAsync(int id);
        Task<MasterKategoriDto?> GetActiveKategoriAsync();
        Task<MasterKategoriDto> SaveKategoriAsync(SaveMasterKategoriDto dto, int? userId);
        Task<bool> DeleteKategoriAsync(int id);
        Task<List<MasterKategoriDetailDto>> GetKategoriDetailsAsync(int kategoriId);
        Task<MasterKategoriDetailDto> SaveKategoriDetailAsync(SaveMasterKategoriDetailDto dto, int? userId);
        Task<bool> DeleteKategoriDetailAsync(int detailId);

        // Periode
        Task<List<PeriodeDto>> GetAllPeriodeAsync(string? status = null);
        Task<PeriodeDto?> GetPeriodeByIdAsync(int id);
        Task<PeriodeDto?> GetActivePeriodeAsync();
        Task<PeriodeDto> CreatePeriodeAsync(CreatePeriodeDto dto, int? userId);
        Task<bool> DeletePeriodeAsync(int id);

        // Penilaian 360 & Kampanye Penilaian
        Task<List<Penilaian360Dto>> GetAllPenilaian360Async();
        Task<List<Penilaian360Dto>> GetPenilaian360ByPeriodeAsync(int periodeId);
        Task<List<Penilaian360Dto>> GetPenilaian360ByUserIdAsync(int userId);
        Task<List<Penilaian360Dto>> GetPenilaian360ByPenilaiAsync(int penilaiUserId);
        Task<List<Penilaian360Dto>> GetTugasEvaluasiKaryawanAsync(int penilaiUserId, int? periodeId);
        Task<Penilaian360Dto> SavePenilaian360Async(SavePenilaian360Dto dto, int? userId);
        Task<bool> DeletePenilaian360Async(int id);
        Task<bool> SebarkanPenilaian360Async(int periodeId, int? userId, CreatePeriodeDto? config = null);
        Task<MonitoringProgresPeriodeDto> GetMonitoringProgresPeriodeAsync(int periodeId);

        // SPK Calculation & Hasil
        Task<List<HasilPenilaianSistemDto>> PreviewHasilPeriodeAsync(int periodeId);
        Task<List<HasilPenilaianSistemDto>> SaveHasilPeriodeAsync(int periodeId, int? userId);
        Task<List<HasilPenilaianSistemDto>> CalculateHasilPeriodeAsync(int periodeId, int? userId);
        Task<List<HasilPenilaianSistemDto>> GetHasilByPeriodeAsync(int periodeId);
        Task<List<HasilPenilaianSistemDto>> GetHasilByUserIdAsync(int userId);
        Task<List<HasilPenilaianSistemDto>> GetHistoryGlobalAsync();
        Task<bool> UpdateCatatanHrdAsync(int id, string catatan, int? userId);
    }
}
