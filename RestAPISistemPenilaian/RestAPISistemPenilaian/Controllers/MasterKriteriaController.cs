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
    public class MasterKriteriaController : ControllerBase
    {
        private readonly ISistemPenilaianService _service;
        private readonly ILogger<MasterKriteriaController> _logger;

        public MasterKriteriaController(ISistemPenilaianService service, ILogger<MasterKriteriaController> logger)
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
            var data = await _service.GetAllKriteriaAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetKriteriaByIdAsync(id);
            if (data == null) return NotFound(new { success = false, message = "Kriteria tidak ditemukan" });
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveMasterKriteriaDto dto)
        {
            try
            {
                var result = await _service.SaveKriteriaAsync(dto, GetCurrentUserId());
                return Ok(new { success = true, message = "Kriteria berhasil disimpan", data = result });
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
                var success = await _service.DeleteKriteriaAsync(id);
                if (!success) return NotFound(new { success = false, message = "Kriteria tidak ditemukan" });
                return Ok(new { success = true, message = "Kriteria berhasil dihapus" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
