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
    public class HasilPenilaianSistemController : ControllerBase
    {
        private readonly ISistemPenilaianService _service;
        private readonly ILogger<HasilPenilaianSistemController> _logger;

        public HasilPenilaianSistemController(ISistemPenilaianService service, ILogger<HasilPenilaianSistemController> logger)
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

        [HttpGet("preview/{periodeId}")]
        public async Task<IActionResult> Preview(int periodeId)
        {
            try
            {
                var result = await _service.PreviewHasilPeriodeAsync(periodeId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat preview SPK periode {PeriodeId}", periodeId);
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest(new { success = false, message = innerMsg });
            }
        }

        [HttpPost("save/{periodeId}")]
        [HttpPost("calculate/{periodeId}")]
        public async Task<IActionResult> Save(int periodeId)
        {
            try
            {
                var result = await _service.SaveHasilPeriodeAsync(periodeId, GetCurrentUserId());
                return Ok(new { success = true, message = "Hasil penilaian kinerja berhasil disimpan ke database", data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat menyimpan hasil SPK periode {PeriodeId}", periodeId);
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest(new { success = false, message = innerMsg });
            }
        }

        [HttpGet("periode/{periodeId}")]
        public async Task<IActionResult> GetByPeriode(int periodeId)
        {
            var data = await _service.GetHasilByPeriodeAsync(periodeId);
            return Ok(data);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var data = await _service.GetHasilByUserIdAsync(userId);
            return Ok(data);
        }

        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory([FromQuery] int? userId)
        {
            int targetId = userId ?? GetCurrentUserId() ?? 0;
            if (targetId <= 0) return BadRequest(new { success = false, message = "User ID tidak valid." });

            var data = await _service.GetHasilByUserIdAsync(targetId);
            return Ok(data);
        }

        [HttpGet("history-global")]
        public async Task<IActionResult> GetHistoryGlobal()
        {
            var data = await _service.GetHistoryGlobalAsync();
            return Ok(data);
        }

        [HttpPut("catatan/{id}")]
        public async Task<IActionResult> UpdateCatatan(int id, [FromBody] UpdateCatatanDto request)
        {
            var success = await _service.UpdateCatatanHrdAsync(id, request.CatatanHRD ?? string.Empty, GetCurrentUserId());
            if (!success) return NotFound(new { success = false, message = "Data hasil evaluasi tidak ditemukan" });
            return Ok(new { success = true, message = "Catatan HRD berhasil disimpan" });
        }
    }

    public class UpdateCatatanDto
    {
        public string? CatatanHRD { get; set; }
    }
}
