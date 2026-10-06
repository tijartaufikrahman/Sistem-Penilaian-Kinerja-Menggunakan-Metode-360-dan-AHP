using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestAPISistemPenilaian.Interfaces;
using RestAPISistemPenilaian.Models.DTOs;
using System.Security.Claims;

namespace RestAPISistemPenilaian.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class Penilaian360Controller : ControllerBase
    {
        private readonly ISistemPenilaianService _service;
        private readonly ILogger<Penilaian360Controller> _logger;

        public Penilaian360Controller(ISistemPenilaianService service, ILogger<Penilaian360Controller> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("id")?.Value
                       ?? User.FindFirst("sub")?.Value;
            return int.TryParse(idClaim, out int id) ? id : null;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllPenilaian360Async();
            return Ok(data);
        }

        [HttpGet("by-periode/{periodeId}")]
        public async Task<IActionResult> GetByPeriode(int periodeId)
        {
            var data = await _service.GetPenilaian360ByPeriodeAsync(periodeId);
            return Ok(data);
        }

        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var data = await _service.GetPenilaian360ByUserIdAsync(userId);
            return Ok(data);
        }

        [HttpGet("my-evaluations")]
        [HttpGet("tugas-saya")]
        public async Task<IActionResult> GetMyEvaluations([FromQuery] int? penilaiId, [FromQuery] int? periodeId)
        {
            int targetPenilaiId = penilaiId ?? GetCurrentUserId() ?? 0;
            if (targetPenilaiId <= 0) return BadRequest(new { success = false, message = "ID Penilai tidak valid." });

            var data = await _service.GetTugasEvaluasiKaryawanAsync(targetPenilaiId, periodeId);
            return Ok(data);
        }

        [HttpPost("sebarkan/{periodeId}")]
        public async Task<IActionResult> Sebarkan(int periodeId)
        {
            try
            {
                var success = await _service.SebarkanPenilaian360Async(periodeId, GetCurrentUserId());
                return Ok(new { success = true, message = "Tugas penilaian 360° berhasil disebarkan ke seluruh karyawan untuk periode ini." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("monitoring-progres/{periodeId}")]
        public async Task<IActionResult> GetMonitoringProgres(int periodeId)
        {
            try
            {
                var data = await _service.GetMonitoringProgresPeriodeAsync(periodeId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [HttpPost("submit")]
        public async Task<IActionResult> Save([FromBody] SavePenilaian360Dto dto)
        {
            try
            {
                if (dto.UserId <= 0 && (!dto.Id.HasValue || dto.Id.Value <= 0))
                    return BadRequest(new { success = false, message = "Karyawan yang dinilai harus dipilih." });

                if (dto.PenilaiUserId <= 0)
                {
                    dto.PenilaiUserId = GetCurrentUserId() ?? 0;
                }

                if (dto.PenilaiUserId <= 0 && (!dto.Id.HasValue || dto.Id.Value <= 0))
                    return BadRequest(new { success = false, message = "Karyawan penilai tidak valid." });

                var result = await _service.SavePenilaian360Async(dto, GetCurrentUserId());
                return Ok(new { success = true, message = "Evaluasi 360 berhasil disimpan", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeletePenilaian360Async(id);
            if (!success) return NotFound(new { success = false, message = "Data penilaian tidak ditemukan" });
            return Ok(new { success = true, message = "Penilaian 360 berhasil dihapus" });
        }
    }
}
