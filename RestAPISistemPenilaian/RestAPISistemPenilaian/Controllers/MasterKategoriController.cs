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
    public class MasterKategoriController : ControllerBase
    {
        private readonly ISistemPenilaianService _service;
        private readonly ILogger<MasterKategoriController> _logger;

        public MasterKategoriController(ISistemPenilaianService service, ILogger<MasterKategoriController> logger)
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
            var data = await _service.GetAllKategoriAsync();
            return Ok(data);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var data = await _service.GetActiveKategoriAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetKategoriByIdAsync(id);
            if (data == null) return NotFound(new { success = false, message = "Skema kategori tidak ditemukan." });
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveMasterKategoriDto dto)
        {
            try
            {
                var result = await _service.SaveKategoriAsync(dto, GetCurrentUserId());
                return Ok(new { success = true, message = "Skema kategori berhasil disimpan.", data = result });
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
                var success = await _service.DeleteKategoriAsync(id);
                if (!success) return NotFound(new { success = false, message = "Skema kategori tidak ditemukan." });
                return Ok(new { success = true, message = "Skema kategori berhasil dihapus." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // DETAIL ITEMS (GRADES)
        // ==========================================
        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var data = await _service.GetKategoriDetailsAsync(id);
            return Ok(data);
        }

        [HttpPost("detail")]
        public async Task<IActionResult> SaveDetail([FromBody] SaveMasterKategoriDetailDto dto)
        {
            try
            {
                var result = await _service.SaveKategoriDetailAsync(dto, GetCurrentUserId());
                return Ok(new { success = true, message = "Item grade kategori berhasil disimpan.", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("detail/{detailId}")]
        public async Task<IActionResult> DeleteDetail(int detailId)
        {
            try
            {
                var success = await _service.DeleteKategoriDetailAsync(detailId);
                if (!success) return NotFound(new { success = false, message = "Item grade kategori tidak ditemukan." });
                return Ok(new { success = true, message = "Item grade kategori berhasil dihapus." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
