using Microsoft.EntityFrameworkCore;
using RestAPISistemPenilaian.Data;
using RestAPISistemPenilaian.Interfaces;
using RestAPISistemPenilaian.Models.DTOs;
using RestAPISistemPenilaian.Models.Entities;

namespace RestAPISistemPenilaian.Services
{
    public class SistemPenilaianService : ISistemPenilaianService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SistemPenilaianService> _logger;

        // Saaty's Random Index table for n=7 is 1.32
        private const double RI_7 = 1.32;
        private const int CRITERIA_COUNT = 7;

        public SistemPenilaianService(AppDbContext context, ILogger<SistemPenilaianService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region 1. AHP CALCULATION & RULES
        public async Task<AhpCalculationResultDto> CalculateAhpAsync(AhpPairwiseRequestDto request, int? userId)
        {
            var matrix = request.Matrix;
            if (matrix == null || matrix.Length != CRITERIA_COUNT)
            {
                return new AhpCalculationResultDto
                {
                    Success = false,
                    Message = "Matriks perbandingan harus berukuran 7x7 (untuk 7 kriteria)."
                };
            }

            int n = CRITERIA_COUNT;
            double[,] A = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                if (matrix[i] == null || matrix[i].Length != n)
                {
                    return new AhpCalculationResultDto
                    {
                        Success = false,
                        Message = $"Baris matriks ke-{i + 1} tidak valid."
                    };
                }
                for (int j = 0; j < n; j++)
                {
                    double val = matrix[i][j];
                    if (val <= 0) val = 1.0;
                    A[i, j] = val;
                }
                A[i, i] = 1.0;
            }

            // Pastikan resiprokalitas A[j, i] = 1 / A[i, j]
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (A[i, j] > 0)
                    {
                        A[j, i] = 1.0 / A[i, j];
                    }
                }
            }

            // 1. Hitung jumlah kolom
            double[] colSum = new double[n];
            for (int j = 0; j < n; j++)
            {
                colSum[j] = 0;
                for (int i = 0; i < n; i++)
                {
                    colSum[j] += A[i, j];
                }
            }

            // 2. Normalisasi matriks dan hitung Bobot Prioritas (Eigen Vector)
            double[][] normalizedMatrix = new double[n][];
            double[] weights = new double[n];

            for (int i = 0; i < n; i++)
            {
                normalizedMatrix[i] = new double[n];
                double rowSumNorm = 0;
                for (int j = 0; j < n; j++)
                {
                    normalizedMatrix[i][j] = Math.Round(A[i, j] / colSum[j], 4);
                    rowSumNorm += A[i, j] / colSum[j];
                }
                weights[i] = Math.Round(rowSumNorm / n, 4);
            }

            // Normalisasi kembali total bobot agar tepat 1.0000
            double totalWeight = weights.Sum();
            if (totalWeight > 0)
            {
                for (int i = 0; i < n; i++)
                {
                    weights[i] = Math.Round(weights[i] / totalWeight, 4);
                }
            }

            // 3. Hitung Vector Jumlah Terbobot (Weighted Sum Vector)
            double[] weightedSum = new double[n];
            for (int i = 0; i < n; i++)
            {
                weightedSum[i] = 0;
                for (int j = 0; j < n; j++)
                {
                    weightedSum[i] += A[i, j] * weights[j];
                }
            }

            // 4. Hitung Lambda Max
            double lambdaMax = 0;
            for (int i = 0; i < n; i++)
            {
                if (weights[i] > 0)
                {
                    lambdaMax += weightedSum[i] / weights[i];
                }
            }
            lambdaMax = Math.Round(lambdaMax / n, 4);

            // 5. Hitung Consistency Index (CI) & Consistency Ratio (CR)
            double ci = (lambdaMax - n) / (n - 1);
            if (ci < 0) ci = 0;
            ci = Math.Round(ci, 4);

            double cr = Math.Round(ci / RI_7, 4);
            bool isConsistent = cr <= 0.1000;

            MasterBobotAHPDto? savedRuleDto = null;

            if (isConsistent && request.SaveIfConsistent)
            {
                string kode = string.IsNullOrWhiteSpace(request.KodeAturan) 
                    ? $"RUL-AHP-{DateTime.UtcNow:yyyyMMddHHmmss}" 
                    : request.KodeAturan.Trim();

                string nama = string.IsNullOrWhiteSpace(request.NamaAturan) 
                    ? $"Aturan Bobot AHP {DateTime.UtcNow:yyyy-MM-dd}" 
                    : request.NamaAturan.Trim();

                // Non-aktifkan aturan lama jika ini diset aktif
                var existingActives = await _context.MasterBobotAHPs.Where(m => m.IsActive).ToListAsync();
                foreach (var ex in existingActives) ex.IsActive = false;

                var newRule = new MasterBobotAHP
                {
                    KodeAturan = kode,
                    NamaAturan = nama,
                    Bobot_KerjasamaTim = (decimal)weights[0],
                    Bobot_KetepatanWaktu = (decimal)weights[1],
                    Bobot_TanggungJawab = (decimal)weights[2],
                    Bobot_Inisiatif = (decimal)weights[3],
                    Bobot_KeterlibatanKerja = (decimal)weights[4],
                    Bobot_InteraksiKolega = (decimal)weights[5],
                    Bobot_TindakLanjut = (decimal)weights[6],
                    ConsistencyRatio_CR = (decimal)cr,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                await _context.MasterBobotAHPs.AddAsync(newRule);
                await _context.SaveChangesAsync();

                savedRuleDto = MapBobotAhpToDto(newRule);
            }

            return new AhpCalculationResultDto
            {
                Success = true,
                Message = isConsistent 
                    ? $"Perbandingan KONSISTEN (CR = {cr:F4} <= 0.1). Bobot sah dan valid."
                    : $"Perbandingan TIDAK KONSISTEN (CR = {cr:F4} > 0.1). Silakan sesuaikan matriks perbandingan.",
                NormalizedMatrix = normalizedMatrix,
                Weights = weights,
                LambdaMax = lambdaMax,
                ConsistencyIndex_CI = ci,
                ConsistencyRatio_CR = cr,
                IsConsistent = isConsistent,
                SavedRule = savedRuleDto
            };
        }
        #endregion

        #region 0. MASTER KRITERIA
        public async Task<List<MasterKriteriaDto>> GetAllKriteriaAsync()
        {
            var list = await _context.MasterKriterias
                .OrderBy(k => k.KodeKriteria)
                .ToListAsync();

            return list.Select(k => new MasterKriteriaDto
            {
                Id = k.Id,
                KodeKriteria = k.KodeKriteria,
                NamaKriteria = k.NamaKriteria,
                Deskripsi = k.Deskripsi,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            }).ToList();
        }

        public async Task<MasterKriteriaDto?> GetKriteriaByIdAsync(int id)
        {
            var k = await _context.MasterKriterias.FindAsync(id);
            if (k == null) return null;

            return new MasterKriteriaDto
            {
                Id = k.Id,
                KodeKriteria = k.KodeKriteria,
                NamaKriteria = k.NamaKriteria,
                Deskripsi = k.Deskripsi,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            };
        }

        public async Task<MasterKriteriaDto> SaveKriteriaAsync(SaveMasterKriteriaDto dto, int? userId)
        {
            MasterKriteria entity;
            if (dto.Id.HasValue && dto.Id.Value > 0)
            {
                entity = await _context.MasterKriterias.FindAsync(dto.Id.Value)
                    ?? throw new KeyNotFoundException("Kriteria tidak ditemukan.");

                entity.KodeKriteria = dto.KodeKriteria.Trim().ToUpper();
                entity.NamaKriteria = dto.NamaKriteria.Trim();
                entity.Deskripsi = dto.Deskripsi?.Trim();
                entity.IsActive = dto.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = userId;
            }
            else
            {
                string kode = string.IsNullOrWhiteSpace(dto.KodeKriteria)
                    ? $"C{await _context.MasterKriterias.CountAsync() + 1}"
                    : dto.KodeKriteria.Trim().ToUpper();

                entity = new MasterKriteria
                {
                    KodeKriteria = kode,
                    NamaKriteria = dto.NamaKriteria.Trim(),
                    Deskripsi = dto.Deskripsi?.Trim(),
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };
                await _context.MasterKriterias.AddAsync(entity);
            }

            await _context.SaveChangesAsync();

            return new MasterKriteriaDto
            {
                Id = entity.Id,
                KodeKriteria = entity.KodeKriteria,
                NamaKriteria = entity.NamaKriteria,
                Deskripsi = entity.Deskripsi,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            };
        }

        public async Task<bool> DeleteKriteriaAsync(int id)
        {
            var entity = await _context.MasterKriterias.FindAsync(id);
            if (entity == null) return false;

            bool isUsedInAhp = await _context.MasterBobotAHPKriterias.AnyAsync(m => m.KriteriaId == id);
            if (isUsedInAhp)
            {
                throw new InvalidOperationException("Kriteria ini tidak dapat dihapus karena sudah digunakan dalam perumusan kebijakan bobot AHP.");
            }

            _context.MasterKriterias.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        #endregion

        #region 1. MASTER BOBOT AHP
        public async Task<List<MasterBobotAHPDto>> GetAllBobotAhpAsync()
        {
            var rules = await _context.MasterBobotAHPs
                .Include(r => r.KriteriaDetails)
                    .ThenInclude(d => d.MasterKriteria)
                .OrderByDescending(r => r.IsActive)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            return rules.Select(MapBobotAhpToDto).ToList();
        }

        public async Task<MasterBobotAHPDto?> GetBobotAhpByIdAsync(int id)
        {
            var rule = await _context.MasterBobotAHPs
                .Include(r => r.KriteriaDetails)
                    .ThenInclude(d => d.MasterKriteria)
                .FirstOrDefaultAsync(r => r.Id == id);

            return rule != null ? MapBobotAhpToDto(rule) : null;
        }

        public async Task<MasterBobotAHPDto?> GetActiveBobotAhpAsync()
        {
            var rule = await _context.MasterBobotAHPs
                .Include(r => r.KriteriaDetails)
                    .ThenInclude(d => d.MasterKriteria)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(r => r.IsActive);

            return rule != null ? MapBobotAhpToDto(rule) : null;
        }

        public async Task<MasterBobotAHPDto> CreateBobotAhpAsync(CreateMasterBobotAHPDto dto, int? userId)
        {
            if (dto.IsActive)
            {
                var existingActives = await _context.MasterBobotAHPs.Where(m => m.IsActive).ToListAsync();
                foreach (var ex in existingActives) ex.IsActive = false;
            }

            string kode = string.IsNullOrWhiteSpace(dto.KodeAturan)
                ? $"RUL-AHP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}"
                : dto.KodeAturan.Trim();

            var entity = new MasterBobotAHP
            {
                KodeAturan = kode,
                NamaAturan = dto.NamaAturan,
                Keterangan = dto.Keterangan,
                Bobot_KerjasamaTim = dto.Bobot_KerjasamaTim,
                Bobot_KetepatanWaktu = dto.Bobot_KetepatanWaktu,
                Bobot_TanggungJawab = dto.Bobot_TanggungJawab,
                Bobot_Inisiatif = dto.Bobot_Inisiatif,
                Bobot_KeterlibatanKerja = dto.Bobot_KeterlibatanKerja,
                Bobot_InteraksiKolega = dto.Bobot_InteraksiKolega,
                Bobot_TindakLanjut = dto.Bobot_TindakLanjut,
                ConsistencyRatio_CR = dto.ConsistencyRatio_CR,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            // Jika payload membawa kriteriaBobot, sinkronkan nilai 7 kriteria default ke kolom entity
            if (dto.KriteriaBobot != null && dto.KriteriaBobot.Any())
            {
                foreach (var kb in dto.KriteriaBobot)
                {
                    if (kb.KriteriaId == 1) entity.Bobot_KerjasamaTim = kb.Bobot;
                    else if (kb.KriteriaId == 2) entity.Bobot_KetepatanWaktu = kb.Bobot;
                    else if (kb.KriteriaId == 3) entity.Bobot_TanggungJawab = kb.Bobot;
                    else if (kb.KriteriaId == 4) entity.Bobot_Inisiatif = kb.Bobot;
                    else if (kb.KriteriaId == 5) entity.Bobot_KeterlibatanKerja = kb.Bobot;
                    else if (kb.KriteriaId == 6) entity.Bobot_InteraksiKolega = kb.Bobot;
                    else if (kb.KriteriaId == 7) entity.Bobot_TindakLanjut = kb.Bobot;
                }
            }

            await _context.MasterBobotAHPs.AddAsync(entity);
            await _context.SaveChangesAsync();

            if (dto.KriteriaBobot != null && dto.KriteriaBobot.Any())
            {
                foreach (var kb in dto.KriteriaBobot)
                {
                    await _context.MasterBobotAHPKriterias.AddAsync(new MasterBobotAHPKriteria
                    {
                        BobotAHPId = entity.Id,
                        KriteriaId = kb.KriteriaId,
                        Bobot = kb.Bobot,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    });
                }
                await _context.SaveChangesAsync();
            }

            return await GetBobotAhpByIdAsync(entity.Id) ?? MapBobotAhpToDto(entity);
        }

        public async Task<bool> SetActiveBobotAhpAsync(int id, int? userId)
        {
            var rule = await _context.MasterBobotAHPs.FindAsync(id);
            if (rule == null) return false;

            var all = await _context.MasterBobotAHPs.ToListAsync();
            foreach (var r in all) r.IsActive = false;

            rule.IsActive = true;
            rule.UpdatedAt = DateTime.UtcNow;
            rule.UpdatedBy = userId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusBobotAhpAsync(int id, bool isActive, int? userId)
        {
            var rule = await _context.MasterBobotAHPs.FindAsync(id);
            if (rule == null) return false;

            rule.IsActive = isActive;
            rule.UpdatedAt = DateTime.UtcNow;
            rule.UpdatedBy = userId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteBobotAhpAsync(int id)
        {
            var rule = await _context.MasterBobotAHPs.FindAsync(id);
            if (rule == null) return false;

            bool isUsedInPeriode = await _context.Periodes.AnyAsync(p => p.BobotAHPId == id);
            if (isUsedInPeriode)
            {
                throw new InvalidOperationException("Kebijakan bobot AHP ini sedang terikat pada siklus periode penilaian dan tidak dapat dihapus.");
            }

            _context.MasterBobotAHPs.Remove(rule);
            await _context.SaveChangesAsync();
            return true;
        }

        private static MasterBobotAHPDto MapBobotAhpToDto(MasterBobotAHP r)
        {
            var dto = new MasterBobotAHPDto
            {
                Id = r.Id,
                KodeAturan = r.KodeAturan,
                NamaAturan = r.NamaAturan,
                Keterangan = r.Keterangan,
                Bobot_KerjasamaTim = r.Bobot_KerjasamaTim,
                Bobot_KetepatanWaktu = r.Bobot_KetepatanWaktu,
                Bobot_TanggungJawab = r.Bobot_TanggungJawab,
                Bobot_Inisiatif = r.Bobot_Inisiatif,
                Bobot_KeterlibatanKerja = r.Bobot_KeterlibatanKerja,
                Bobot_InteraksiKolega = r.Bobot_InteraksiKolega,
                Bobot_TindakLanjut = r.Bobot_TindakLanjut,
                ConsistencyRatio_CR = r.ConsistencyRatio_CR,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            };

            if (r.KriteriaDetails != null && r.KriteriaDetails.Any())
            {
                dto.KriteriaDetails = r.KriteriaDetails.Select(kd => new BobotKriteriaDetailDto
                {
                    KriteriaId = kd.KriteriaId,
                    KodeKriteria = kd.MasterKriteria?.KodeKriteria ?? $"C{kd.KriteriaId}",
                    NamaKriteria = kd.MasterKriteria?.NamaKriteria ?? "-",
                    Bobot = kd.Bobot
                }).ToList();
            }

            return dto;
        }
        #endregion

        #region 2. MASTER KATEGORI (HEADER - DETAIL)
        public async Task<List<MasterKategoriDto>> GetAllKategoriAsync()
        {
            var list = await _context.MasterKategoris
                .Include(k => k.Details)
                .OrderByDescending(k => k.IsActive)
                .ThenByDescending(k => k.CreatedAt)
                .ToListAsync();

            return list.Select(MapKategoriToDto).ToList();
        }

        public async Task<MasterKategoriDto?> GetKategoriByIdAsync(int id)
        {
            var k = await _context.MasterKategoris
                .Include(k => k.Details)
                .FirstOrDefaultAsync(k => k.Id == id);

            return k != null ? MapKategoriToDto(k) : null;
        }

        public async Task<MasterKategoriDto?> GetActiveKategoriAsync()
        {
            var k = await _context.MasterKategoris
                .Include(k => k.Details)
                .OrderByDescending(k => k.CreatedAt)
                .FirstOrDefaultAsync(k => k.IsActive);

            return k != null ? MapKategoriToDto(k) : null;
        }

        public async Task<MasterKategoriDto> SaveKategoriAsync(SaveMasterKategoriDto dto, int? userId)
        {
            MasterKategori entity;
            if (dto.Id.HasValue && dto.Id.Value > 0)
            {
                entity = await _context.MasterKategoris
                    .Include(k => k.Details)
                    .FirstOrDefaultAsync(k => k.Id == dto.Id.Value)
                    ?? throw new KeyNotFoundException("Skema kategori tidak ditemukan.");

                if (!string.IsNullOrWhiteSpace(dto.KodeKategori))
                    entity.KodeKategori = dto.KodeKategori.Trim().ToUpper();

                entity.NamaKategori = dto.NamaKategori.Trim();
                entity.Deskripsi = dto.Deskripsi?.Trim();
                entity.IsActive = dto.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = userId;
            }
            else
            {
                string kode = string.IsNullOrWhiteSpace(dto.KodeKategori)
                    ? $"KAT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}"
                    : dto.KodeKategori.Trim().ToUpper();

                entity = new MasterKategori
                {
                    KodeKategori = kode,
                    NamaKategori = dto.NamaKategori.Trim(),
                    Deskripsi = dto.Deskripsi?.Trim(),
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                await _context.MasterKategoris.AddAsync(entity);
                await _context.SaveChangesAsync();

                if (dto.Details != null && dto.Details.Any())
                {
                    foreach (var d in dto.Details)
                    {
                        await _context.MasterKategoriDetails.AddAsync(new MasterKategoriDetail
                        {
                            KategoriId = entity.Id,
                            NamaGrade = d.NamaGrade.Trim(),
                            NilaiMin = d.NilaiMin,
                            NilaiMax = d.NilaiMax,
                            Rekomendasi = d.Rekomendasi.Trim(),
                            Keterangan = d.Keterangan?.Trim(),
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = userId
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return await GetKategoriByIdAsync(entity.Id) ?? MapKategoriToDto(entity);
        }

        public async Task<bool> DeleteKategoriAsync(int id)
        {
            var k = await _context.MasterKategoris.FindAsync(id);
            if (k == null) return false;

            bool isUsed = await _context.Periodes.AnyAsync(p => p.KategoriId == id);
            if (isUsed)
            {
                throw new InvalidOperationException("Skema kategori ini sedang terikat pada siklus penilaian kinerja dan tidak dapat dihapus.");
            }

            _context.MasterKategoris.Remove(k);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<MasterKategoriDetailDto>> GetKategoriDetailsAsync(int kategoriId)
        {
            var list = await _context.MasterKategoriDetails
                .Where(d => d.KategoriId == kategoriId)
                .OrderByDescending(d => d.NilaiMin)
                .ToListAsync();

            return list.Select(d => new MasterKategoriDetailDto
            {
                Id = d.Id,
                KategoriId = d.KategoriId,
                NamaGrade = d.NamaGrade,
                NilaiMin = d.NilaiMin,
                NilaiMax = d.NilaiMax,
                Rekomendasi = d.Rekomendasi,
                Keterangan = d.Keterangan,
                CreatedAt = d.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            }).ToList();
        }

        public async Task<MasterKategoriDetailDto> SaveKategoriDetailAsync(SaveMasterKategoriDetailDto dto, int? userId)
        {
            MasterKategoriDetail entity;
            if (dto.Id.HasValue && dto.Id.Value > 0)
            {
                entity = await _context.MasterKategoriDetails.FindAsync(dto.Id.Value)
                    ?? throw new KeyNotFoundException("Detail kategori tidak ditemukan.");

                entity.NamaGrade = dto.NamaGrade.Trim();
                entity.NilaiMin = dto.NilaiMin;
                entity.NilaiMax = dto.NilaiMax;
                entity.Rekomendasi = dto.Rekomendasi.Trim();
                entity.Keterangan = dto.Keterangan?.Trim();
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = userId;
            }
            else
            {
                entity = new MasterKategoriDetail
                {
                    KategoriId = dto.KategoriId,
                    NamaGrade = dto.NamaGrade.Trim(),
                    NilaiMin = dto.NilaiMin,
                    NilaiMax = dto.NilaiMax,
                    Rekomendasi = dto.Rekomendasi.Trim(),
                    Keterangan = dto.Keterangan?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };
                await _context.MasterKategoriDetails.AddAsync(entity);
            }

            await _context.SaveChangesAsync();

            return new MasterKategoriDetailDto
            {
                Id = entity.Id,
                KategoriId = entity.KategoriId,
                NamaGrade = entity.NamaGrade,
                NilaiMin = entity.NilaiMin,
                NilaiMax = entity.NilaiMax,
                Rekomendasi = entity.Rekomendasi,
                Keterangan = entity.Keterangan,
                CreatedAt = entity.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            };
        }

        public async Task<bool> DeleteKategoriDetailAsync(int detailId)
        {
            var d = await _context.MasterKategoriDetails.FindAsync(detailId);
            if (d == null) return false;

            bool isUsed = await _context.HasilPenilaianSistems.AnyAsync(h => h.KategoriDetailId == detailId);
            if (isUsed)
            {
                throw new InvalidOperationException("Grade kategori ini sudah tercatat dalam hasil penilaian sistem dan tidak dapat dihapus.");
            }

            _context.MasterKategoriDetails.Remove(d);
            await _context.SaveChangesAsync();
            return true;
        }

        private static MasterKategoriDto MapKategoriToDto(MasterKategori k)
        {
            var dto = new MasterKategoriDto
            {
                Id = k.Id,
                KodeKategori = k.KodeKategori,
                NamaKategori = k.NamaKategori,
                Deskripsi = k.Deskripsi,
                IsActive = k.IsActive,
                TotalDetail = k.Details?.Count ?? 0,
                CreatedAt = k.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            };

            if (k.Details != null && k.Details.Any())
            {
                dto.Details = k.Details
                    .OrderByDescending(d => d.NilaiMin)
                    .Select(d => new MasterKategoriDetailDto
                    {
                        Id = d.Id,
                        KategoriId = d.KategoriId,
                        NamaGrade = d.NamaGrade,
                        NilaiMin = d.NilaiMin,
                        NilaiMax = d.NilaiMax,
                        Rekomendasi = d.Rekomendasi,
                        Keterangan = d.Keterangan,
                        CreatedAt = d.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                    }).ToList();
            }

            return dto;
        }
        #endregion

        #region 3. PERIODE
        public async Task<List<PeriodeDto>> GetAllPeriodeAsync(string? status = null)
        {
            var query = _context.Periodes
                .Include(p => p.MasterBobotAHP)
                .Include(p => p.MasterKategori)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var list = await query
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            var totalUsers = await _context.Users.CountAsync(u => u.RoleId == 2 && u.IsActive);

            var periodeIds = list.Select(p => p.Id).ToList();

            // Ambil data penugasan per periode
            var tasksData = await _context.Penilaian360s
                .Where(t => t.PeriodeId.HasValue && periodeIds.Contains(t.PeriodeId.Value))
                .Select(t => new { t.PeriodeId, t.PenilaiUserId, t.UserId, t.StatusPengisian })
                .ToListAsync();

            return list.Select(p =>
            {
                var periodTasks = tasksData.Where(t => t.PeriodeId == p.Id).ToList();
                int totalTugas = periodTasks.Count;
                int tugasSelesai = periodTasks.Count(t => t.StatusPengisian == "Selesai");
                int totalPenilai = periodTasks.Select(t => t.PenilaiUserId).Distinct().Count();
                int penilaiMengisi = periodTasks
                    .GroupBy(t => t.PenilaiUserId)
                    .Count(g => g.Any(x => x.StatusPengisian == "Selesai"));
                int penilaiTuntas = periodTasks
                    .GroupBy(t => t.PenilaiUserId)
                    .Count(g => g.All(x => x.StatusPengisian == "Selesai"));

                int totalKaryawan = periodTasks.Select(t => t.UserId).Distinct().Count();
                int karyawanSelesai = periodTasks
                    .GroupBy(t => t.UserId)
                    .Count(g => g.All(x => x.StatusPengisian == "Selesai"));

                double persentase = totalTugas > 0 ? Math.Round(((double)tugasSelesai / totalTugas) * 100, 1) : 0;

                return new PeriodeDto
                {
                    Id = p.Id,
                    KodePeriode = p.KodePeriode,
                    BobotAHPId = p.BobotAHPId,
                    NamaAturanAHP = p.MasterBobotAHP?.NamaAturan ?? "-",
                    KategoriId = p.KategoriId,
                    NamaKategori = p.MasterKategori?.NamaKategori ?? "-",
                    NamaPeriode = p.NamaPeriode,
                    Status = p.Status,
                    TanggalMulai = p.TanggalMulai.ToString("yyyy-MM-dd"),
                    TanggalSelesai = p.TanggalSelesai.ToString("yyyy-MM-dd"),
                    CreatedAt = p.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    TotalPeserta = totalUsers,
                    TotalEvaluasiSelesai = _context.HasilPenilaianSistems.Count(h => h.PeriodeId == p.Id),
                    TotalAkunMengisi = penilaiMengisi,
                    TotalAkunPenilai = totalPenilai,
                    TotalTugasSelesai = tugasSelesai,
                    TotalTugas = totalTugas,
                    TotalKaryawanDinilai = totalKaryawan,
                    TotalKaryawanSelesai = karyawanSelesai,
                    PersentasePengisian = persentase
                };
            }).ToList();
        }

        public async Task<PeriodeDto?> GetPeriodeByIdAsync(int id)
        {
            var p = await _context.Periodes
                .Include(p => p.MasterBobotAHP)
                    .ThenInclude(b => b!.KriteriaDetails)
                        .ThenInclude(kd => kd.MasterKriteria)
                .Include(p => p.MasterKategori)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (p == null) return null;

            var periodTasks = await _context.Penilaian360s
                .Where(t => t.PeriodeId == id)
                .Select(t => new { t.PenilaiUserId, t.UserId, t.StatusPengisian })
                .ToListAsync();

            int totalTugas = periodTasks.Count;
            int tugasSelesai = periodTasks.Count(t => t.StatusPengisian == "Selesai");
            int totalPenilai = periodTasks.Select(t => t.PenilaiUserId).Distinct().Count();
            int penilaiMengisi = periodTasks
                .GroupBy(t => t.PenilaiUserId)
                .Count(g => g.Any(x => x.StatusPengisian == "Selesai"));
            int penilaiTuntas = periodTasks
                .GroupBy(t => t.PenilaiUserId)
                .Count(g => g.All(x => x.StatusPengisian == "Selesai"));

            int totalKaryawan = periodTasks.Select(t => t.UserId).Distinct().Count();
            int karyawanSelesai = periodTasks
                .GroupBy(t => t.UserId)
                .Count(g => g.All(x => x.StatusPengisian == "Selesai"));

            double persentase = totalTugas > 0 ? Math.Round(((double)tugasSelesai / totalTugas) * 100, 1) : 0;

            return new PeriodeDto
            {
                Id = p.Id,
                KodePeriode = p.KodePeriode,
                BobotAHPId = p.BobotAHPId,
                NamaAturanAHP = p.MasterBobotAHP?.NamaAturan ?? "-",
                BobotAHP = p.MasterBobotAHP != null ? MapBobotAhpToDto(p.MasterBobotAHP) : null,
                KategoriId = p.KategoriId,
                NamaKategori = p.MasterKategori?.NamaKategori ?? "-",
                NamaPeriode = p.NamaPeriode,
                Status = p.Status,
                TanggalMulai = p.TanggalMulai.ToString("yyyy-MM-dd"),
                TanggalSelesai = p.TanggalSelesai.ToString("yyyy-MM-dd"),
                CreatedAt = p.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                TotalAkunMengisi = penilaiMengisi,
                TotalAkunPenilai = totalPenilai,
                TotalTugasSelesai = tugasSelesai,
                TotalTugas = totalTugas,
                TotalKaryawanDinilai = totalKaryawan,
                TotalKaryawanSelesai = karyawanSelesai,
                PersentasePengisian = persentase
            };
        }

        public async Task<PeriodeDto?> GetActivePeriodeAsync()
        {
            var p = await _context.Periodes
                .Include(p => p.MasterBobotAHP)
                .Include(p => p.MasterKategori)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(p => p.Status == "Berjalan" || p.Status == "Aktif");

            if (p == null) return null;

            return await GetPeriodeByIdAsync(p.Id);
        }

        public async Task<PeriodeDto> CreatePeriodeAsync(CreatePeriodeDto dto, int? userId)
        {
            string kode = string.IsNullOrWhiteSpace(dto.KodePeriode)
                ? $"PRD-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}"
                : dto.KodePeriode;

            int katId = dto.KategoriId;
            if (katId <= 0)
            {
                katId = await _context.MasterKategoris
                    .Where(k => k.IsActive)
                    .OrderBy(k => k.Id)
                    .Select(k => k.Id)
                    .FirstOrDefaultAsync();
            }

            var entity = new Periode
            {
                KodePeriode = kode,
                BobotAHPId = dto.BobotAHPId,
                KategoriId = katId,
                NamaPeriode = dto.NamaPeriode,
                Status = dto.Status,
                TanggalMulai = dto.TanggalMulai,
                TanggalSelesai = dto.TanggalSelesai,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };
            await _context.Periodes.AddAsync(entity);
            await _context.SaveChangesAsync();

            // Otomatis buat tugas penilaian 360° jika status periode adalah Berjalan
            if (entity.Status == "Berjalan")
            {
                try
                {
                    await SebarkanPenilaian360Async(entity.Id, userId, dto);
                }
                catch
                {
                    // Abaikan jika belum ada karyawan aktif atau sudah terbuat
                }
            }

            return await GetPeriodeByIdAsync(entity.Id) ?? throw new Exception("Gagal menyimpan periode");
        }

        public async Task<bool> DeletePeriodeAsync(int id)
        {
            var p = await _context.Periodes.FindAsync(id);
            if (p == null) return false;

            // Hapus data anak terlebih dahulu agar tidak terjadi foreign key constraint violation (Error 500)
            var related360 = await _context.Penilaian360s.Where(x => x.PeriodeId == id).ToListAsync();
            if (related360.Any())
            {
                _context.Penilaian360s.RemoveRange(related360);
            }

            var relatedHasil = await _context.HasilPenilaianSistems.Where(x => x.PeriodeId == id).ToListAsync();
            if (relatedHasil.Any())
            {
                _context.HasilPenilaianSistems.RemoveRange(relatedHasil);
            }

            _context.Periodes.Remove(p);
            await _context.SaveChangesAsync();
            return true;
        }
        #endregion

        #region 4. PENILAIAN 360
        public async Task<List<Penilaian360Dto>> GetAllPenilaian360Async()
        {
            var list = await _context.Penilaian360s
                .Include(p => p.User)
                .Include(p => p.Periode)
                .OrderByDescending(p => p.TanggalPenilaian)
                .ToListAsync();

            var usersMap = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.NamaLengkap);

            return list.Select(p => new Penilaian360Dto
            {
                Id = p.Id,
                KodePenilaian = p.KodePenilaian,
                PeriodeId = p.PeriodeId,
                NamaPeriode = p.Periode?.NamaPeriode ?? "-",
                UserId = p.UserId,
                NamaKaryawanDinilai = p.User?.NamaLengkap ?? "-",
                KodeKaryawanDinilai = p.User?.KodeKaryawan ?? "-",
                PenilaiUserId = p.PenilaiUserId,
                NamaPenilai = usersMap.ContainsKey(p.PenilaiUserId) ? usersMap[p.PenilaiUserId] : "-",
                TipePenilai = p.TipePenilai,
                BobotPersen = p.BobotPersen,
                kerjasama_tim = p.kerjasama_tim,
                ketepatan_waktu = p.ketepatan_waktu,
                tanggung_jawab = p.tanggung_jawab,
                inisiatif = p.inisiatif,
                keterlibatan_kerja = p.keterlibatan_kerja,
                interaksi_kolega = p.interaksi_kolega,
                tindak_lanjut = p.tindak_lanjut,
                TanggalPenilaian = p.TanggalPenilaian.ToString("yyyy-MM-dd HH:mm"),
                StatusPengisian = p.StatusPengisian
            }).ToList();
        }

        public async Task<List<Penilaian360Dto>> GetPenilaian360ByPeriodeAsync(int periodeId)
        {
            var list = await _context.Penilaian360s
                .Include(p => p.User)
                .Include(p => p.Periode)
                .Where(p => p.PeriodeId == periodeId)
                .OrderByDescending(p => p.TanggalPenilaian)
                .ToListAsync();

            var usersMap = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.NamaLengkap);

            return list.Select(p => new Penilaian360Dto
            {
                Id = p.Id,
                KodePenilaian = p.KodePenilaian,
                PeriodeId = p.PeriodeId,
                NamaPeriode = p.Periode?.NamaPeriode ?? "-",
                UserId = p.UserId,
                NamaKaryawanDinilai = p.User?.NamaLengkap ?? "-",
                KodeKaryawanDinilai = p.User?.KodeKaryawan ?? "-",
                PenilaiUserId = p.PenilaiUserId,
                NamaPenilai = usersMap.ContainsKey(p.PenilaiUserId) ? usersMap[p.PenilaiUserId] : "-",
                TipePenilai = p.TipePenilai,
                BobotPersen = p.BobotPersen,
                kerjasama_tim = p.kerjasama_tim,
                ketepatan_waktu = p.ketepatan_waktu,
                tanggung_jawab = p.tanggung_jawab,
                inisiatif = p.inisiatif,
                keterlibatan_kerja = p.keterlibatan_kerja,
                interaksi_kolega = p.interaksi_kolega,
                tindak_lanjut = p.tindak_lanjut,
                TanggalPenilaian = p.TanggalPenilaian.ToString("yyyy-MM-dd HH:mm"),
                StatusPengisian = p.StatusPengisian
            }).ToList();
        }

        public async Task<List<Penilaian360Dto>> GetPenilaian360ByUserIdAsync(int userId)
        {
            var list = await _context.Penilaian360s
                .Include(p => p.User)
                .Include(p => p.Periode)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.TanggalPenilaian)
                .ToListAsync();

            var usersMap = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.NamaLengkap);

            return list.Select(p => new Penilaian360Dto
            {
                Id = p.Id,
                KodePenilaian = p.KodePenilaian,
                PeriodeId = p.PeriodeId,
                NamaPeriode = p.Periode?.NamaPeriode ?? "-",
                UserId = p.UserId,
                NamaKaryawanDinilai = p.User?.NamaLengkap ?? "-",
                KodeKaryawanDinilai = p.User?.KodeKaryawan ?? "-",
                PenilaiUserId = p.PenilaiUserId,
                NamaPenilai = usersMap.ContainsKey(p.PenilaiUserId) ? usersMap[p.PenilaiUserId] : "-",
                TipePenilai = p.TipePenilai,
                BobotPersen = p.BobotPersen,
                kerjasama_tim = p.kerjasama_tim,
                ketepatan_waktu = p.ketepatan_waktu,
                tanggung_jawab = p.tanggung_jawab,
                inisiatif = p.inisiatif,
                keterlibatan_kerja = p.keterlibatan_kerja,
                interaksi_kolega = p.interaksi_kolega,
                tindak_lanjut = p.tindak_lanjut,
                TanggalPenilaian = p.TanggalPenilaian.ToString("yyyy-MM-dd HH:mm"),
                StatusPengisian = p.StatusPengisian
            }).ToList();
        }

        public async Task<List<Penilaian360Dto>> GetPenilaian360ByPenilaiAsync(int penilaiUserId)
        {
            return await GetTugasEvaluasiKaryawanAsync(penilaiUserId, null);
        }

        public async Task<List<Penilaian360Dto>> GetTugasEvaluasiKaryawanAsync(int penilaiUserId, int? periodeId)
        {
            int targetPeriodeId = 0;
            if (periodeId.HasValue && periodeId.Value > 0)
            {
                targetPeriodeId = periodeId.Value;
            }
            else
            {
                var activePeriode = await _context.Periodes
                    .OrderByDescending(pr => pr.CreatedAt)
                    .FirstOrDefaultAsync(pr => pr.Status == "Berjalan" || pr.Status == "Aktif");
                if (activePeriode != null)
                {
                    targetPeriodeId = activePeriode.Id;
                }
            }

            // Jika ada periode target, pastikan tugas 360 sudah digenerate (otomatis tanpa tombol sebarkan)
            if (targetPeriodeId > 0)
            {
                bool tasksExist = await _context.Penilaian360s.AnyAsync(p => p.PeriodeId == targetPeriodeId);
                if (!tasksExist)
                {
                    try
                    {
                        await SebarkanPenilaian360Async(targetPeriodeId, null);
                    }
                    catch
                    {
                        // Abaikan jika user aktif belum mencukupi
                    }
                }
            }

            var query = _context.Penilaian360s
                .Include(p => p.User)
                .Include(p => p.Periode)
                .Where(p => p.PenilaiUserId == penilaiUserId);

            if (targetPeriodeId > 0)
            {
                query = query.Where(p => p.PeriodeId == targetPeriodeId || p.PeriodeId == null);
            }

            var list = await query
                .OrderBy(p => p.StatusPengisian == "Selesai" ? 1 : 0)
                .ThenBy(p => p.TipePenilai)
                .ToListAsync();

            var usersMap = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.NamaLengkap);

            return list.Select(p => new Penilaian360Dto
            {
                Id = p.Id,
                KodePenilaian = p.KodePenilaian,
                PeriodeId = p.PeriodeId,
                NamaPeriode = p.Periode?.NamaPeriode ?? "Periode Aktif",
                UserId = p.UserId,
                NamaKaryawanDinilai = p.User?.NamaLengkap ?? "-",
                KodeKaryawanDinilai = p.User?.KodeKaryawan ?? "-",
                PenilaiUserId = p.PenilaiUserId,
                NamaPenilai = usersMap.ContainsKey(p.PenilaiUserId) ? usersMap[p.PenilaiUserId] : "-",
                TipePenilai = p.TipePenilai,
                BobotPersen = p.BobotPersen,
                kerjasama_tim = p.kerjasama_tim,
                ketepatan_waktu = p.ketepatan_waktu,
                tanggung_jawab = p.tanggung_jawab,
                inisiatif = p.inisiatif,
                keterlibatan_kerja = p.keterlibatan_kerja,
                interaksi_kolega = p.interaksi_kolega,
                tindak_lanjut = p.tindak_lanjut,
                TanggalPenilaian = p.TanggalPenilaian.ToString("yyyy-MM-dd HH:mm"),
                StatusPengisian = p.StatusPengisian
            }).ToList();
        }

        public async Task<Penilaian360Dto> SavePenilaian360Async(SavePenilaian360Dto dto, int? userId)
        {
            Penilaian360 entity;
            if (dto.Id.HasValue && dto.Id.Value > 0)
            {
                entity = await _context.Penilaian360s.FindAsync(dto.Id.Value)
                    ?? throw new KeyNotFoundException("Penilaian 360 tidak ditemukan");
                
                if (dto.PeriodeId.HasValue && dto.PeriodeId.Value > 0)
                {
                    entity.PeriodeId = dto.PeriodeId.Value;
                }
                entity.kerjasama_tim = dto.kerjasama_tim;
                entity.ketepatan_waktu = dto.ketepatan_waktu;
                entity.tanggung_jawab = dto.tanggung_jawab;
                entity.inisiatif = dto.inisiatif;
                entity.keterlibatan_kerja = dto.keterlibatan_kerja;
                entity.interaksi_kolega = dto.interaksi_kolega;
                entity.tindak_lanjut = dto.tindak_lanjut;
                entity.StatusPengisian = dto.StatusPengisian;
                if (dto.StatusPengisian == "Selesai")
                {
                    entity.TanggalPenilaian = DateTime.UtcNow;
                }
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = userId;
            }
            else
            {
                string kode = string.IsNullOrWhiteSpace(dto.KodePenilaian)
                    ? $"EVL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}"
                    : dto.KodePenilaian;

                entity = new Penilaian360
                {
                    KodePenilaian = kode,
                    PeriodeId = dto.PeriodeId,
                    UserId = dto.UserId,
                    PenilaiUserId = dto.PenilaiUserId,
                    TipePenilai = dto.TipePenilai,
                    BobotPersen = dto.BobotPersen,
                    kerjasama_tim = dto.kerjasama_tim,
                    ketepatan_waktu = dto.ketepatan_waktu,
                    tanggung_jawab = dto.tanggung_jawab,
                    inisiatif = dto.inisiatif,
                    keterlibatan_kerja = dto.keterlibatan_kerja,
                    interaksi_kolega = dto.interaksi_kolega,
                    tindak_lanjut = dto.tindak_lanjut,
                    TanggalPenilaian = DateTime.UtcNow,
                    StatusPengisian = dto.StatusPengisian,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };
                await _context.Penilaian360s.AddAsync(entity);
            }

            await _context.SaveChangesAsync();

            var userDinilai = await _context.Users.FindAsync(entity.UserId);
            var userPenilai = await _context.Users.FindAsync(entity.PenilaiUserId);
            var periode = entity.PeriodeId.HasValue ? await _context.Periodes.FindAsync(entity.PeriodeId.Value) : null;

            return new Penilaian360Dto
            {
                Id = entity.Id,
                KodePenilaian = entity.KodePenilaian,
                PeriodeId = entity.PeriodeId,
                NamaPeriode = periode?.NamaPeriode ?? "-",
                UserId = entity.UserId,
                NamaKaryawanDinilai = userDinilai?.NamaLengkap ?? "-",
                KodeKaryawanDinilai = userDinilai?.KodeKaryawan ?? "-",
                PenilaiUserId = entity.PenilaiUserId,
                NamaPenilai = userPenilai?.NamaLengkap ?? "-",
                TipePenilai = entity.TipePenilai,
                BobotPersen = entity.BobotPersen,
                kerjasama_tim = entity.kerjasama_tim,
                ketepatan_waktu = entity.ketepatan_waktu,
                tanggung_jawab = entity.tanggung_jawab,
                inisiatif = entity.inisiatif,
                keterlibatan_kerja = entity.keterlibatan_kerja,
                interaksi_kolega = entity.interaksi_kolega,
                tindak_lanjut = entity.tindak_lanjut,
                TanggalPenilaian = entity.TanggalPenilaian.ToString("yyyy-MM-dd HH:mm"),
                StatusPengisian = entity.StatusPengisian
            };
        }

        public async Task<bool> DeletePenilaian360Async(int id)
        {
            var p = await _context.Penilaian360s.FindAsync(id);
            if (p == null) return false;

            _context.Penilaian360s.Remove(p);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SebarkanPenilaian360Async(int periodeId, int? userId, CreatePeriodeDto? config = null)
        {
            var periode = await _context.Periodes.FindAsync(periodeId);
            if (periode == null) throw new KeyNotFoundException("Periode penilaian tidak ditemukan.");

            // 1. Tentukan Karyawan Target (Siapa yang Dinilai)
            var usersQuery = _context.Users.Where(u => u.IsActive);

            // Filter sasaran berdasarkan Role jika dipilih
            if (config != null && config.SkemaPenilai == "ROLE_BASED" && config.TargetRoleId.HasValue && config.TargetRoleId.Value > 0)
            {
                usersQuery = usersQuery.Where(u => u.RoleId == config.TargetRoleId.Value);
            }
            // Filter sasaran berdasarkan Karyawan Tertentu (Manual Checklist)
            else if (config != null && config.SkemaPenilai == "CUSTOM_USER" && config.TargetUserIds != null && config.TargetUserIds.Any())
            {
                usersQuery = usersQuery.Where(u => config.TargetUserIds.Contains(u.Id));
            }
            else
            {
                // Default ALL / ALL_360 / HRD_ONLY:
                bool excludeHrd = config == null || config.ExcludeHrdFromTarget;
                if (excludeHrd)
                {
                    usersQuery = usersQuery.Where(u => u.RoleId != 1);
                }
            }

            var karyawans = await usersQuery.OrderBy(u => u.Id).ToListAsync();

            if (!karyawans.Any())
            {
                throw new InvalidOperationException("Tidak ada karyawan aktif untuk disebarkan penilaian.");
            }

            var hrdUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 1 && u.IsActive)
                ?? karyawans.First();

            var existingTasks = await _context.Penilaian360s
                .Where(p => p.PeriodeId == periodeId)
                .ToListAsync();

            var newTasks = new List<Penilaian360>();

            // Konfigurasi Relasi Penilai
            bool includePeer = config == null || (config.SkemaPenilai != "HRD_ONLY" && config.IncludePeerReview);
            bool includeSupervisor = config == null || config.IncludeSupervisorReview || config.SkemaPenilai == "HRD_ONLY";

            decimal supervisorWeight = (includePeer && includeSupervisor) ? 60.00m : (includeSupervisor ? 100.00m : 0.00m);
            decimal totalPeerWeight = (includePeer && includeSupervisor) ? 40.00m : (includePeer ? 100.00m : 0.00m);

            foreach (var target in karyawans)
            {
                bool isTargetHrd = (hrdUser != null && target.Id == hrdUser.Id);

                if (!isTargetHrd)
                {
                    // Target adalah karyawan bawahan / staf

                    // 1. Peer-Assessment (Rekan Kerja sesama karyawan saling menilai satu sama lain)
                    if (includePeer)
                    {
                        var peers = karyawans.Where(k => k.Id != target.Id && (hrdUser == null || k.Id != hrdUser.Id)).ToList();
                        if (peers.Any())
                        {
                            decimal peerWeight = Math.Round(totalPeerWeight / peers.Count, 2);
                            if (peerWeight <= 0) peerWeight = totalPeerWeight;

                            foreach (var peer in peers)
                            {
                                bool hasPeer = existingTasks.Any(e => e.UserId == target.Id && e.PenilaiUserId == peer.Id && e.TipePenilai == "Rekan");
                                if (!hasPeer)
                                {
                                    newTasks.Add(new Penilaian360
                                    {
                                        KodePenilaian = $"EVL-PEER-P{periodeId}-{peer.KodeKaryawan}TO{target.KodeKaryawan}",
                                        PeriodeId = periodeId,
                                        UserId = target.Id,
                                        PenilaiUserId = peer.Id,
                                        TipePenilai = "Rekan",
                                        BobotPersen = peerWeight,
                                        TanggalPenilaian = DateTime.UtcNow,
                                        StatusPengisian = "Draft",
                                        CreatedAt = DateTime.UtcNow,
                                        CreatedBy = userId
                                    });
                                }
                            }
                        }
                    }

                    // 2. Supervisor / HRD Assessment (HRD menilai Karyawan sebagai Atasan)
                    if (includeSupervisor && hrdUser != null)
                    {
                        bool hasSupervisor = existingTasks.Any(e => e.UserId == target.Id && e.PenilaiUserId == hrdUser.Id && e.TipePenilai == "Atasan");
                        if (!hasSupervisor)
                        {
                            newTasks.Add(new Penilaian360
                            {
                                KodePenilaian = $"EVL-SUPV-P{periodeId}-{hrdUser.KodeKaryawan}TO{target.KodeKaryawan}",
                                PeriodeId = periodeId,
                                UserId = target.Id,
                                PenilaiUserId = hrdUser.Id,
                                TipePenilai = "Atasan",
                                BobotPersen = supervisorWeight,
                                TanggalPenilaian = DateTime.UtcNow,
                                StatusPengisian = "Draft",
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = userId
                            });
                        }
                    }
                }
                else
                {
                    // Target adalah akun HRD itu sendiri
                    // HRD dievaluasi balik oleh para karyawan aktif (evaluasi 360 arah)
                    // HRD tidak menilai dirinya sendiri sebagai atasan
                    var evaluators = karyawans.Where(k => k.Id != target.Id).ToList();
                    if (!evaluators.Any())
                    {
                        evaluators = await _context.Users.Where(u => u.IsActive && u.Id != target.Id).ToListAsync();
                    }

                    if (evaluators.Any())
                    {
                        decimal hrdPeerWeight = Math.Round(100.00m / evaluators.Count, 2);
                        if (hrdPeerWeight <= 0) hrdPeerWeight = 100.00m;

                        foreach (var ev in evaluators)
                        {
                            bool hasTask = existingTasks.Any(e => e.UserId == target.Id && e.PenilaiUserId == ev.Id);
                            if (!hasTask)
                            {
                                newTasks.Add(new Penilaian360
                                {
                                    KodePenilaian = $"EVL-REV360-P{periodeId}-{ev.KodeKaryawan}TO{target.KodeKaryawan}",
                                    PeriodeId = periodeId,
                                    UserId = target.Id,
                                    PenilaiUserId = ev.Id,
                                    TipePenilai = "Rekan",
                                    BobotPersen = hrdPeerWeight,
                                    TanggalPenilaian = DateTime.UtcNow,
                                    StatusPengisian = "Draft",
                                    CreatedAt = DateTime.UtcNow,
                                    CreatedBy = userId
                                });
                            }
                        }
                    }
                }
            }

            if (newTasks.Any())
            {
                await _context.Penilaian360s.AddRangeAsync(newTasks);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<MonitoringProgresPeriodeDto> GetMonitoringProgresPeriodeAsync(int periodeId)
        {
            var periode = await _context.Periodes.FindAsync(periodeId)
                ?? throw new KeyNotFoundException("Periode tidak ditemukan.");

            var tasks = await _context.Penilaian360s
                .Where(p => p.PeriodeId == periodeId)
                .ToListAsync();

            int total = tasks.Count;
            int selesai = tasks.Count(t => t.StatusPengisian == "Selesai");
            int belum = total - selesai;
            double pct = total > 0 ? Math.Round(((double)selesai / total) * 100, 1) : 0;

            int totalPenilai = tasks.Select(t => t.PenilaiUserId).Distinct().Count();
            int penilaiMengisi = tasks
                .GroupBy(t => t.PenilaiUserId)
                .Count(g => g.Any(x => x.StatusPengisian == "Selesai"));
            int penilaiTuntas = tasks
                .GroupBy(t => t.PenilaiUserId)
                .Count(g => g.All(x => x.StatusPengisian == "Selesai"));

            return new MonitoringProgresPeriodeDto
            {
                PeriodeId = periodeId,
                NamaPeriode = periode.NamaPeriode,
                StatusPeriode = periode.Status,
                TotalTugasEvaluasi = total,
                TotalSelesai = selesai,
                TotalBelumSelesai = belum,
                PersentaseSelesai = pct,
                SiapDihitung = selesai > 0,
                TotalAkunPenilai = totalPenilai,
                TotalAkunMengisi = penilaiMengisi,
                TotalAkunTuntas = penilaiTuntas
            };
        }
        #endregion

        #region 5. SPK CALCULATION & HASIL SISTEM
        public async Task<List<HasilPenilaianSistemDto>> PreviewHasilPeriodeAsync(int periodeId)
        {
            // Jika sudah pernah disimpan di database untuk periode ini, kembalikan hasil yang tersimpan
            var existingDbList = await GetHasilByPeriodeAsync(periodeId);
            if (existingDbList != null && existingDbList.Any())
            {
                return existingDbList;
            }

            // Jika belum pernah disimpan di DB, hitung simulasi realtime di memori tanpa SaveChanges
            var periode = await _context.Periodes
                .Include(p => p.MasterBobotAHP)
                .Include(p => p.MasterKategori)
                .FirstOrDefaultAsync(p => p.Id == periodeId)
                ?? throw new KeyNotFoundException("Periode penilaian tidak ditemukan.");

            var bobot = await _context.MasterBobotAHPs
                .Include(b => b.KriteriaDetails)
                    .ThenInclude(kd => kd.MasterKriteria)
                .FirstOrDefaultAsync(b => b.Id == periode.BobotAHPId)
                ?? await _context.MasterBobotAHPs
                    .Include(b => b.KriteriaDetails)
                        .ThenInclude(kd => kd.MasterKriteria)
                    .FirstOrDefaultAsync(m => m.IsActive)
                ?? throw new InvalidOperationException("Master Bobot AHP untuk periode ini belum ditentukan.");

            // Ekstrak bobot kriteria secara dinamis: prioritaskan tabel relasi KriteriaDetails jika terisi, fallback ke kolom flat
            decimal wKerjasama = bobot.Bobot_KerjasamaTim;
            decimal wWaktu = bobot.Bobot_KetepatanWaktu;
            decimal wTanggungJwb = bobot.Bobot_TanggungJawab;
            decimal wInisiatif = bobot.Bobot_Inisiatif;
            decimal wKeterlibatan = bobot.Bobot_KeterlibatanKerja;
            decimal wInteraksi = bobot.Bobot_InteraksiKolega;
            decimal wTindakLanjut = bobot.Bobot_TindakLanjut;

            if (bobot.KriteriaDetails != null && bobot.KriteriaDetails.Any(kd => kd.Bobot > 0))
            {
                var kdMap = bobot.KriteriaDetails.ToDictionary(kd => kd.KriteriaId, kd => kd.Bobot);
                if (kdMap.TryGetValue(1, out var k1)) wKerjasama = k1;
                if (kdMap.TryGetValue(2, out var k2)) wWaktu = k2;
                if (kdMap.TryGetValue(3, out var k3)) wTanggungJwb = k3;
                if (kdMap.TryGetValue(4, out var k4)) wInisiatif = k4;
                if (kdMap.TryGetValue(5, out var k5)) wKeterlibatan = k5;
                if (kdMap.TryGetValue(6, out var k6)) wInteraksi = k6;
                if (kdMap.TryGetValue(7, out var k7)) wTindakLanjut = k7;
            }

            var kategoriDetails = await _context.MasterKategoriDetails
                .Where(d => d.KategoriId == periode.KategoriId)
                .OrderByDescending(k => k.NilaiMin)
                .ToListAsync();

            if (!kategoriDetails.Any())
            {
                kategoriDetails = await _context.MasterKategoriDetails
                    .Where(d => d.MasterKategori != null && d.MasterKategori.IsActive)
                    .OrderByDescending(k => k.NilaiMin)
                    .ToListAsync();
            }

            if (!kategoriDetails.Any())
            {
                throw new InvalidOperationException("Detail kategori untuk skema periode ini belum dikonfigurasi.");
            }

            // Ambil semua target yang dievaluasi pada periode ini berdasarkan penugasan Penilaian360
            var targetUserIds = await _context.Penilaian360s
                .Where(p => p.PeriodeId == periodeId)
                .Select(p => p.UserId)
                .Distinct()
                .ToListAsync();

            List<User> karyawans;
            if (targetUserIds.Any())
            {
                karyawans = await _context.Users
                    .Where(u => targetUserIds.Contains(u.Id))
                    .OrderBy(u => u.Id)
                    .ToListAsync();
            }
            else
            {
                karyawans = await _context.Users
                    .Where(u => u.RoleId == 2 && u.IsActive)
                    .OrderBy(u => u.Id)
                    .ToListAsync();
            }

            var previewList = new List<HasilPenilaianSistemDto>();

            foreach (var k in karyawans)
            {
                var evals = await _context.Penilaian360s
                    .Where(p => p.UserId == k.Id && (p.PeriodeId == periodeId || p.PeriodeId == null) && p.StatusPengisian == "Selesai")
                    .ToListAsync();

                var totalExpected = await _context.Penilaian360s
                    .CountAsync(p => p.UserId == k.Id && p.PeriodeId == periodeId);

                bool sudahAdaPenilaian = evals.Any();

                decimal avgKerjasama = 0;
                decimal avgWaktu = 0;
                decimal avgTanggungJwb = 0;
                decimal avgInisiatif = 0;
                decimal avgKeterlibatan = 0;
                decimal avgInteraksi = 0;
                decimal avgTindakLanjut = 0;

                if (sudahAdaPenilaian)
                {
                    decimal totalBobotPenilai = evals.Sum(e => e.BobotPersen);
                    if (totalBobotPenilai <= 0) totalBobotPenilai = 100.0m;

                    avgKerjasama = Math.Round(evals.Sum(e => e.kerjasama_tim * e.BobotPersen) / totalBobotPenilai, 2);
                    avgWaktu = Math.Round(evals.Sum(e => e.ketepatan_waktu * e.BobotPersen) / totalBobotPenilai, 2);
                    avgTanggungJwb = Math.Round(evals.Sum(e => e.tanggung_jawab * e.BobotPersen) / totalBobotPenilai, 2);
                    avgInisiatif = Math.Round(evals.Sum(e => e.inisiatif * e.BobotPersen) / totalBobotPenilai, 2);
                    avgKeterlibatan = Math.Round(evals.Sum(e => e.keterlibatan_kerja * e.BobotPersen) / totalBobotPenilai, 2);
                    avgInteraksi = Math.Round(evals.Sum(e => e.interaksi_kolega * e.BobotPersen) / totalBobotPenilai, 2);
                    avgTindakLanjut = Math.Round(evals.Sum(e => e.tindak_lanjut * e.BobotPersen) / totalBobotPenilai, 2);
                }

                decimal skorAkhir = sudahAdaPenilaian
                    ? Math.Round(
                        (avgKerjasama * wKerjasama) +
                        (avgWaktu * wWaktu) +
                        (avgTanggungJwb * wTanggungJwb) +
                        (avgInisiatif * wInisiatif) +
                        (avgKeterlibatan * wKeterlibatan) +
                        (avgInteraksi * wInteraksi) +
                        (avgTindakLanjut * wTindakLanjut), 2)
                    : 0m;

                var matchedKategori = kategoriDetails.FirstOrDefault(c => skorAkhir >= c.NilaiMin && skorAkhir <= c.NilaiMax)
                    ?? kategoriDetails.Last();

                string namaKat = sudahAdaPenilaian ? matchedKategori.NamaGrade : "Belum Dinilai";
                string rekVal = sudahAdaPenilaian ? matchedKategori.Rekomendasi : "Menunggu Penilaian";

                var kriteriaNilai = BuildKriteriaNilaiList(
                    avgKerjasama, avgWaktu, avgTanggungJwb, avgInisiatif,
                    avgKeterlibatan, avgInteraksi, avgTindakLanjut, bobot);

                previewList.Add(new HasilPenilaianSistemDto
                {
                    Id = 0,
                    KodeHasil = $"PREVIEW-P{periodeId}-{k.KodeKaryawan}",
                    PeriodeId = periodeId,
                    NamaPeriode = periode.NamaPeriode,
                    UserId = k.Id,
                    KodeKaryawan = k.KodeKaryawan,
                    NamaKaryawan = k.NamaLengkap,
                    Email = k.Email,
                    KategoriDetailId = matchedKategori.Id,
                    KategoriId = matchedKategori.KategoriId,
                    NamaKategori = namaKat,
                    Rekomendasi = rekVal,
                    Rata2_KerjasamaTim = avgKerjasama,
                    Rata2_KetepatanWaktu = avgWaktu,
                    Rata2_TanggungJawab = avgTanggungJwb,
                    Rata2_Inisiatif = avgInisiatif,
                    Rata2_KeterlibatanKerja = avgKeterlibatan,
                    Rata2_InteraksiKolega = avgInteraksi,
                    Rata2_TindakLanjut = avgTindakLanjut,
                    SkorAkhirAHP = skorAkhir,
                    Ranking = 0,
                    CatatanHRD = null,
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
                    TotalEvaluasiMasuk = evals.Count,
                    TotalEvaluasiDiharapkan = totalExpected,
                    SudahAdaPenilaian = sudahAdaPenilaian,
                    KriteriaNilai = kriteriaNilai
                });
            }

            var ranked = previewList
                .OrderByDescending(h => h.SudahAdaPenilaian)
                .ThenByDescending(h => h.SkorAkhirAHP)
                .ToList();
            for (int r = 0; r < ranked.Count; r++)
            {
                ranked[r].Ranking = r + 1;
            }

            return ranked;
        }

        public async Task<List<HasilPenilaianSistemDto>> SaveHasilPeriodeAsync(int periodeId, int? userId)
        {
            return await CalculateHasilPeriodeAsync(periodeId, userId);
        }

        public async Task<List<HasilPenilaianSistemDto>> CalculateHasilPeriodeAsync(int periodeId, int? userId)
        {
            var periode = await _context.Periodes
                .Include(p => p.MasterBobotAHP)
                .Include(p => p.MasterKategori)
                .FirstOrDefaultAsync(p => p.Id == periodeId)
                ?? throw new KeyNotFoundException("Periode penilaian tidak ditemukan.");

            var bobot = await _context.MasterBobotAHPs
                .Include(b => b.KriteriaDetails)
                    .ThenInclude(kd => kd.MasterKriteria)
                .FirstOrDefaultAsync(b => b.Id == periode.BobotAHPId)
                ?? await _context.MasterBobotAHPs
                    .Include(b => b.KriteriaDetails)
                        .ThenInclude(kd => kd.MasterKriteria)
                    .FirstOrDefaultAsync(m => m.IsActive)
                ?? throw new InvalidOperationException("Master Bobot AHP untuk periode ini belum ditentukan.");

            // Ekstrak bobot kriteria secara dinamis: prioritaskan tabel relasi KriteriaDetails jika terisi, fallback ke kolom flat
            decimal wKerjasama = bobot.Bobot_KerjasamaTim;
            decimal wWaktu = bobot.Bobot_KetepatanWaktu;
            decimal wTanggungJwb = bobot.Bobot_TanggungJawab;
            decimal wInisiatif = bobot.Bobot_Inisiatif;
            decimal wKeterlibatan = bobot.Bobot_KeterlibatanKerja;
            decimal wInteraksi = bobot.Bobot_InteraksiKolega;
            decimal wTindakLanjut = bobot.Bobot_TindakLanjut;

            if (bobot.KriteriaDetails != null && bobot.KriteriaDetails.Any(kd => kd.Bobot > 0))
            {
                var kdMap = bobot.KriteriaDetails.ToDictionary(kd => kd.KriteriaId, kd => kd.Bobot);
                if (kdMap.TryGetValue(1, out var k1)) wKerjasama = k1;
                if (kdMap.TryGetValue(2, out var k2)) wWaktu = k2;
                if (kdMap.TryGetValue(3, out var k3)) wTanggungJwb = k3;
                if (kdMap.TryGetValue(4, out var k4)) wInisiatif = k4;
                if (kdMap.TryGetValue(5, out var k5)) wKeterlibatan = k5;
                if (kdMap.TryGetValue(6, out var k6)) wInteraksi = k6;
                if (kdMap.TryGetValue(7, out var k7)) wTindakLanjut = k7;
            }

            var kategoriDetails = await _context.MasterKategoriDetails
                .Where(d => d.KategoriId == periode.KategoriId)
                .OrderByDescending(k => k.NilaiMin)
                .ToListAsync();

            if (!kategoriDetails.Any())
            {
                kategoriDetails = await _context.MasterKategoriDetails
                    .Where(d => d.MasterKategori != null && d.MasterKategori.IsActive)
                    .OrderByDescending(k => k.NilaiMin)
                    .ToListAsync();
            }

            if (!kategoriDetails.Any())
            {
                throw new InvalidOperationException("Detail kategori untuk skema periode ini belum dikonfigurasi.");
            }

            // Ambil semua target yang dievaluasi pada periode ini berdasarkan penugasan Penilaian360
            var targetUserIds = await _context.Penilaian360s
                .Where(p => p.PeriodeId == periodeId)
                .Select(p => p.UserId)
                .Distinct()
                .ToListAsync();

            List<User> karyawans;
            if (targetUserIds.Any())
            {
                karyawans = await _context.Users
                    .Where(u => targetUserIds.Contains(u.Id))
                    .OrderBy(u => u.Id)
                    .ToListAsync();
            }
            else
            {
                karyawans = await _context.Users
                    .Where(u => u.RoleId == 2 && u.IsActive)
                    .OrderBy(u => u.Id)
                    .ToListAsync();
            }

            var hasilList = new List<HasilPenilaianSistem>();

            foreach (var k in karyawans)
            {
                // Ambil data 360 yang berstatus "Selesai" untuk karyawan ini pada periode ini
                var evals = await _context.Penilaian360s
                    .Where(p => p.UserId == k.Id && (p.PeriodeId == periodeId || p.PeriodeId == null) && p.StatusPengisian == "Selesai")
                    .ToListAsync();

                decimal avgKerjasama = 0;
                decimal avgWaktu = 0;
                decimal avgTanggungJwb = 0;
                decimal avgInisiatif = 0;
                decimal avgKeterlibatan = 0;
                decimal avgInteraksi = 0;
                decimal avgTindakLanjut = 0;

                if (evals.Any())
                {
                    decimal totalBobotPenilai = evals.Sum(e => e.BobotPersen);
                    if (totalBobotPenilai <= 0) totalBobotPenilai = 100.0m;

                    avgKerjasama = Math.Round(evals.Sum(e => e.kerjasama_tim * e.BobotPersen) / totalBobotPenilai, 2);
                    avgWaktu = Math.Round(evals.Sum(e => e.ketepatan_waktu * e.BobotPersen) / totalBobotPenilai, 2);
                    avgTanggungJwb = Math.Round(evals.Sum(e => e.tanggung_jawab * e.BobotPersen) / totalBobotPenilai, 2);
                    avgInisiatif = Math.Round(evals.Sum(e => e.inisiatif * e.BobotPersen) / totalBobotPenilai, 2);
                    avgKeterlibatan = Math.Round(evals.Sum(e => e.keterlibatan_kerja * e.BobotPersen) / totalBobotPenilai, 2);
                    avgInteraksi = Math.Round(evals.Sum(e => e.interaksi_kolega * e.BobotPersen) / totalBobotPenilai, 2);
                    avgTindakLanjut = Math.Round(evals.Sum(e => e.tindak_lanjut * e.BobotPersen) / totalBobotPenilai, 2);
                }

                // Perkalian skor akhir dengan bobot AHP
                decimal skorAkhir = Math.Round(
                    (avgKerjasama * wKerjasama) +
                    (avgWaktu * wWaktu) +
                    (avgTanggungJwb * wTanggungJwb) +
                    (avgInisiatif * wInisiatif) +
                    (avgKeterlibatan * wKeterlibatan) +
                    (avgInteraksi * wInteraksi) +
                    (avgTindakLanjut * wTindakLanjut), 2);

                // Tentukan KategoriDetailId
                var matchedKategori = kategoriDetails.FirstOrDefault(c => skorAkhir >= c.NilaiMin && skorAkhir <= c.NilaiMax)
                    ?? kategoriDetails.Last();

                var existingHasil = await _context.HasilPenilaianSistems
                    .FirstOrDefaultAsync(h => h.PeriodeId == periodeId && h.UserId == k.Id);

                if (existingHasil != null)
                {
                    existingHasil.Rata2_KerjasamaTim = avgKerjasama;
                    existingHasil.Rata2_KetepatanWaktu = avgWaktu;
                    existingHasil.Rata2_TanggungJawab = avgTanggungJwb;
                    existingHasil.Rata2_Inisiatif = avgInisiatif;
                    existingHasil.Rata2_KeterlibatanKerja = avgKeterlibatan;
                    existingHasil.Rata2_InteraksiKolega = avgInteraksi;
                    existingHasil.Rata2_TindakLanjut = avgTindakLanjut;
                    existingHasil.SkorAkhirAHP = skorAkhir;
                    existingHasil.KategoriDetailId = matchedKategori.Id;
                    existingHasil.UpdatedAt = DateTime.UtcNow;
                    existingHasil.UpdatedBy = userId;
                    hasilList.Add(existingHasil);
                }
                else
                {
                    var newHasil = new HasilPenilaianSistem
                    {
                        KodeHasil = $"RPT-P{periodeId}-{DateTime.UtcNow.Year}-{k.KodeKaryawan}",
                        PeriodeId = periodeId,
                        UserId = k.Id,
                        KategoriDetailId = matchedKategori.Id,
                        Rata2_KerjasamaTim = avgKerjasama,
                        Rata2_KetepatanWaktu = avgWaktu,
                        Rata2_TanggungJawab = avgTanggungJwb,
                        Rata2_Inisiatif = avgInisiatif,
                        Rata2_KeterlibatanKerja = avgKeterlibatan,
                        Rata2_InteraksiKolega = avgInteraksi,
                        Rata2_TindakLanjut = avgTindakLanjut,
                        SkorAkhirAHP = skorAkhir,
                        Ranking = 0, // Akan dihitung setelah diurutkan
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };
                    await _context.HasilPenilaianSistems.AddAsync(newHasil);
                    hasilList.Add(newHasil);
                }
            }

            // Urutkan ranking menurun berdasarkan SkorAkhirAHP
            var rankedList = hasilList.OrderByDescending(h => h.SkorAkhirAHP).ToList();
            for (int r = 0; r < rankedList.Count; r++)
            {
                rankedList[r].Ranking = r + 1;
            }

            // Update status periode menjadi Selesai (karena penilaian kinerja resmi telah disimpan/dikunci)
            periode.Status = "Selesai";
            periode.UpdatedAt = DateTime.UtcNow;
            periode.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            return await GetHasilByPeriodeAsync(periodeId);
        }

        public async Task<List<HasilPenilaianSistemDto>> GetHasilByPeriodeAsync(int periodeId)
        {
            var list = await _context.HasilPenilaianSistems
                .Include(h => h.User)
                .Include(h => h.Periode)
                    .ThenInclude(p => p!.MasterBobotAHP)
                        .ThenInclude(b => b!.KriteriaDetails)
                            .ThenInclude(kd => kd.MasterKriteria)
                .Include(h => h.MasterKategoriDetail)
                    .ThenInclude(d => d!.MasterKategori)
                .Where(h => h.PeriodeId == periodeId)
                .OrderBy(h => h.Ranking)
                .ToListAsync();

            if (!list.Any()) return new List<HasilPenilaianSistemDto>();

            var tasks = await _context.Penilaian360s
                .Where(p => p.PeriodeId == periodeId)
                .ToListAsync();

            var taskStats = tasks
                .GroupBy(p => p.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        TotalDiharapkan = g.Count(),
                        TotalMasuk = g.Count(x => x.StatusPengisian == "Selesai")
                    }
                );

            var dtoList = list.Select(MapHasilToDto).ToList();
            foreach (var dto in dtoList)
            {
                if (taskStats.TryGetValue(dto.UserId, out var st))
                {
                    dto.TotalEvaluasiMasuk = st.TotalMasuk;
                    dto.TotalEvaluasiDiharapkan = st.TotalDiharapkan;
                    dto.SudahAdaPenilaian = st.TotalMasuk > 0;
                }
            }
            return dtoList;
        }

        public async Task<List<HasilPenilaianSistemDto>> GetHasilByUserIdAsync(int userId)
        {
            var list = await _context.HasilPenilaianSistems
                .Include(h => h.User)
                .Include(h => h.Periode)
                    .ThenInclude(p => p!.MasterBobotAHP)
                        .ThenInclude(b => b!.KriteriaDetails)
                            .ThenInclude(kd => kd.MasterKriteria)
                .Include(h => h.MasterKategoriDetail)
                    .ThenInclude(d => d!.MasterKategori)
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.Periode!.CreatedAt)
                .ThenByDescending(h => h.PeriodeId)
                .ToListAsync();

            return list.Select(MapHasilToDto).ToList();
        }

        public async Task<List<HasilPenilaianSistemDto>> GetHistoryGlobalAsync()
        {
            var list = await _context.HasilPenilaianSistems
                .Include(h => h.User)
                .Include(h => h.Periode)
                    .ThenInclude(p => p!.MasterBobotAHP)
                        .ThenInclude(b => b!.KriteriaDetails)
                            .ThenInclude(kd => kd.MasterKriteria)
                .Include(h => h.MasterKategoriDetail)
                    .ThenInclude(d => d!.MasterKategori)
                .OrderByDescending(h => h.Periode!.CreatedAt)
                .ThenByDescending(h => h.PeriodeId)
                .ThenBy(h => h.Ranking)
                .ToListAsync();

            return list.Select(MapHasilToDto).ToList();
        }

        public async Task<bool> UpdateCatatanHrdAsync(int id, string catatan, int? userId)
        {
            var item = await _context.HasilPenilaianSistems.FindAsync(id);
            if (item == null) return false;

            item.CatatanHRD = catatan;
            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedBy = userId;

            await _context.SaveChangesAsync();
            return true;
        }

        private static HasilPenilaianSistemDto MapHasilToDto(HasilPenilaianSistem h)
        {
            var dto = new HasilPenilaianSistemDto
            {
                Id = h.Id,
                KodeHasil = h.KodeHasil,
                PeriodeId = h.PeriodeId,
                NamaPeriode = h.Periode?.NamaPeriode ?? "-",
                UserId = h.UserId,
                KodeKaryawan = h.User?.KodeKaryawan ?? "-",
                NamaKaryawan = h.User?.NamaLengkap ?? "-",
                Email = h.User?.Email ?? "-",
                KategoriDetailId = h.KategoriDetailId,
                KategoriId = h.MasterKategoriDetail?.KategoriId ?? 0,
                NamaKategori = h.MasterKategoriDetail?.NamaGrade ?? "-",
                Rekomendasi = h.MasterKategoriDetail?.Rekomendasi ?? "-",
                Rata2_KerjasamaTim = h.Rata2_KerjasamaTim,
                Rata2_KetepatanWaktu = h.Rata2_KetepatanWaktu,
                Rata2_TanggungJawab = h.Rata2_TanggungJawab,
                Rata2_Inisiatif = h.Rata2_Inisiatif,
                Rata2_KeterlibatanKerja = h.Rata2_KeterlibatanKerja,
                Rata2_InteraksiKolega = h.Rata2_InteraksiKolega,
                Rata2_TindakLanjut = h.Rata2_TindakLanjut,
                SkorAkhirAHP = h.SkorAkhirAHP,
                Ranking = h.Ranking,
                CatatanHRD = h.CatatanHRD,
                CreatedAt = h.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                SudahAdaPenilaian = true,
                TotalEvaluasiMasuk = 1,
                TotalEvaluasiDiharapkan = 1
            };

            dto.KriteriaNilai = BuildKriteriaNilaiList(
                h.Rata2_KerjasamaTim, h.Rata2_KetepatanWaktu, h.Rata2_TanggungJawab,
                h.Rata2_Inisiatif, h.Rata2_KeterlibatanKerja, h.Rata2_InteraksiKolega,
                h.Rata2_TindakLanjut, h.Periode?.MasterBobotAHP);

            return dto;
        }

        private static List<KriteriaNilaiItemDto> BuildKriteriaNilaiList(
            decimal avgKerjasama, decimal avgWaktu, decimal avgTanggungJwb, decimal avgInisiatif,
            decimal avgKeterlibatan, decimal avgInteraksi, decimal avgTindakLanjut,
            MasterBobotAHP? bobot)
        {
            var list = new List<KriteriaNilaiItemDto>();
            if (bobot == null) return list;

            if (bobot.KriteriaDetails != null && bobot.KriteriaDetails.Any(kd => kd.Bobot > 0))
            {
                var activeDetails = bobot.KriteriaDetails.Where(kd => kd.Bobot > 0).OrderBy(kd => kd.KriteriaId).ToList();
                foreach (var kd in activeDetails)
                {
                    decimal val = kd.KriteriaId switch
                    {
                        1 => avgKerjasama,
                        2 => avgWaktu,
                        3 => avgTanggungJwb,
                        4 => avgInisiatif,
                        5 => avgKeterlibatan,
                        6 => avgInteraksi,
                        7 => avgTindakLanjut,
                        _ => 0m
                    };

                    string kode = kd.MasterKriteria?.KodeKriteria ?? $"C{kd.KriteriaId}";
                    string nama = kd.MasterKriteria?.NamaKriteria ?? (kd.KriteriaId switch
                    {
                        1 => "Kerjasama Tim",
                        2 => "Ketepatan Waktu",
                        3 => "Tanggung Jawab",
                        4 => "Inisiatif",
                        5 => "Keterlibatan Kerja",
                        6 => "Interaksi Kolega",
                        7 => "Tindak Lanjut",
                        _ => $"Kriteria {kd.KriteriaId}"
                    });

                    list.Add(new KriteriaNilaiItemDto
                    {
                        KriteriaId = kd.KriteriaId,
                        KodeKriteria = kode,
                        NamaKriteria = nama,
                        NilaiRataRata = val,
                        Bobot = kd.Bobot,
                        NilaiTertimbang = Math.Round(val * kd.Bobot, 2)
                    });
                }
            }
            else
            {
                var defs = new (int Id, string Kode, string Nama, decimal Bobot, decimal Val)[]
                {
                    (1, "C1", "Kerjasama Tim", bobot.Bobot_KerjasamaTim, avgKerjasama),
                    (2, "C2", "Ketepatan Waktu", bobot.Bobot_KetepatanWaktu, avgWaktu),
                    (3, "C3", "Tanggung Jawab", bobot.Bobot_TanggungJawab, avgTanggungJwb),
                    (4, "C4", "Inisiatif", bobot.Bobot_Inisiatif, avgInisiatif),
                    (5, "C5", "Keterlibatan Kerja", bobot.Bobot_KeterlibatanKerja, avgKeterlibatan),
                    (6, "C6", "Interaksi Kolega", bobot.Bobot_InteraksiKolega, avgInteraksi),
                    (7, "C7", "Tindak Lanjut", bobot.Bobot_TindakLanjut, avgTindakLanjut)
                };

                foreach (var d in defs)
                {
                    if (d.Bobot > 0)
                    {
                        list.Add(new KriteriaNilaiItemDto
                        {
                            KriteriaId = d.Id,
                            KodeKriteria = d.Kode,
                            NamaKriteria = d.Nama,
                            NilaiRataRata = d.Val,
                            Bobot = d.Bobot,
                            NilaiTertimbang = Math.Round(d.Val * d.Bobot, 2)
                        });
                    }
                }
            }

            return list;
        }
        #endregion
    }
}
