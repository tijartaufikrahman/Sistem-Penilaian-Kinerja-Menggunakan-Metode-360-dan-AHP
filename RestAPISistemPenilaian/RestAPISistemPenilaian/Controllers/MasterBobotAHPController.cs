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
    public class MasterBobotAHPController : ControllerBase
    {
        private readonly ISistemPenilaianService _service;
        private readonly ILogger<MasterBobotAHPController> _logger;

        public MasterBobotAHPController(ISistemPenilaianService service, ILogger<MasterBobotAHPController> logger)
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
            var data = await _service.GetAllBobotAhpAsync();
            return Ok(data);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var data = await _service.GetActiveBobotAhpAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetBobotAhpByIdAsync(id);
            if (data == null) return NotFound(new { success = false, message = "Aturan bobot AHP tidak ditemukan" });
            return Ok(data);
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate([FromBody] AhpPairwiseRequestDto request)
        {
            try
            {
                var result = await _service.CalculateAhpAsync(request, GetCurrentUserId());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat kalkulasi AHP");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMasterBobotAHPDto dto)
        {
            try
            {
                var result = await _service.CreateBobotAhpAsync(dto, GetCurrentUserId());
                return Ok(new { success = true, message = "Berhasil membuat aturan bobot AHP baru", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("set-active/{id}")]
        [HttpPost("{id}/activate")]
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> SetActive(int id)
        {
            var success = await _service.SetActiveBobotAhpAsync(id, GetCurrentUserId());
            if (!success) return NotFound(new { success = false, message = "Aturan tidak ditemukan" });
            return Ok(new { success = true, message = "Aturan bobot AHP berhasil diaktifkan" });
        }

        [HttpPut("{id}/status")]
        [HttpPost("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateBobotStatusDto dto)
        {
            var success = await _service.UpdateStatusBobotAhpAsync(id, dto.IsActive, GetCurrentUserId());
            if (!success) return NotFound(new { success = false, message = "Kebijakan bobot AHP tidak ditemukan" });
            string statusStr = dto.IsActive ? "Aktif" : "Tidak Aktif";
            return Ok(new { success = true, message = $"Status kebijakan bobot AHP berhasil diubah menjadi {statusStr}" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _service.DeleteBobotAhpAsync(id);
                if (!success) return NotFound(new { success = false, message = "Kebijakan bobot AHP tidak ditemukan" });
                return Ok(new { success = true, message = "Kebijakan bobot AHP berhasil dihapus" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
