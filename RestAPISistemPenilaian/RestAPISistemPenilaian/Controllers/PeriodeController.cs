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
    public class PeriodeController : ControllerBase
    {
        private readonly ISistemPenilaianService _service;
        private readonly ILogger<PeriodeController> _logger;

        public PeriodeController(ISistemPenilaianService service, ILogger<PeriodeController> logger)
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
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var data = await _service.GetAllPeriodeAsync(status);
            return Ok(data);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var data = await _service.GetActivePeriodeAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetPeriodeByIdAsync(id);
            if (data == null) return NotFound(new { success = false, message = "Periode tidak ditemukan" });
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePeriodeDto dto)
        {
            try
            {
                var result = await _service.CreatePeriodeAsync(dto, GetCurrentUserId());
                return Ok(new { success = true, message = "Penilaian kinerja baru berhasil dibuka", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _service.DeletePeriodeAsync(id);
                if (!success) return NotFound(new { success = false, message = "Periode tidak ditemukan" });
                return Ok(new { success = true, message = "Periode berhasil dihapus" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
