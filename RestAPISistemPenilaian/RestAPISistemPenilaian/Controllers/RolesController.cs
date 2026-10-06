using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestAPISistemPenilaian.Interfaces;
using RestAPISistemPenilaian.Models.DTOs;
using RestAPISistemPenilaian.Models.Entities;

namespace RestAPISistemPenilaian.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly IRoleRepository _roleRepo;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IRoleRepository roleRepo, ILogger<RolesController> logger)
        {
            _roleRepo = roleRepo;
            _logger = logger;
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var count = await _roleRepo.CountAsync();
            return Ok(new { count });
        }

        [HttpGet]
        [HttpGet("/api/admin/GetRoles")]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _roleRepo.GetAllAsync();
            var result = roles
                .OrderBy(r => r.NamaRole)
                .Select(r => new {
                    r.Id,
                    r.NamaRole,
                    Deskripsi = r.Deskripsi ?? "-"
                });

            return Ok(result);
        }

        [HttpPost]
        [HttpPost("/api/admin/CreateRole")]
        public async Task<IActionResult> Create([FromForm] CreateRoleDto model)
        {
            if (string.IsNullOrWhiteSpace(model.NamaRole))
                return Ok(new { success = false, message = "Nama role wajib diisi." });

            var existing = await _roleRepo.GetAllAsync();
            if (existing.Any(r => r.NamaRole.Equals(model.NamaRole, StringComparison.OrdinalIgnoreCase)))
                return Ok(new { success = false, message = "Nama role sudah ada." });

            var role = new Role
            {
                NamaRole = model.NamaRole.Trim(),
                Deskripsi = model.Deskripsi?.Trim()
            };

            await _roleRepo.AddAsync(role);
            await _roleRepo.SaveChangesAsync();

            return Ok(new { success = true, message = "Berhasil menambah role baru." });
        }

        [HttpPost("{id}")]
        [HttpPut("{id}")]
        [HttpPost("/api/admin/EditRole/{id?}")]
        public async Task<IActionResult> Edit([FromRoute] int? id, [FromForm] EditRoleDto model)
        {
            var targetId = id.HasValue && id.Value > 0 ? id.Value : model.Id;

            if (targetId <= 0)
                return Ok(new { success = false, message = "ID tidak valid." });

            var current = await _roleRepo.GetByIdAsync(targetId);
            if (current == null)
                return Ok(new { success = false, message = "Role tidak ditemukan." });

            if (string.IsNullOrWhiteSpace(model.NamaRole))
                return Ok(new { success = false, message = "Nama role wajib diisi." });

            var allRoles = await _roleRepo.GetAllAsync();
            if (allRoles.Any(r => r.NamaRole.Equals(model.NamaRole, StringComparison.OrdinalIgnoreCase) && r.Id != targetId))
                return Ok(new { success = false, message = "Nama role sudah digunakan." });

            current.NamaRole = model.NamaRole.Trim();
            current.Deskripsi = model.Deskripsi?.Trim();

            await _roleRepo.UpdateAsync(current);
            await _roleRepo.SaveChangesAsync();

            return Ok(new { success = true, message = "Berhasil memperbarui role." });
        }

        [HttpPost("delete")]
        [HttpDelete("{id}")]
        [HttpPost("/api/admin/DeleteRole/{id?}")]
        public async Task<IActionResult> Delete([FromRoute] int? id, [FromForm] DeleteRequestDto? formDto)
        {
            var targetId = id.HasValue && id.Value > 0 ? id.Value : (formDto?.Id ?? 0);
            if (targetId <= 0)
                return Ok(new { success = false, message = "ID tidak valid." });

            var hasUsers = await _roleRepo.HasUsersAsync(targetId);
            if (hasUsers)
                return Ok(new { success = false, message = "Tidak bisa menghapus role yang masih digunakan oleh user." });

            await _roleRepo.DeleteAsync(targetId);
            await _roleRepo.SaveChangesAsync();

            return Ok(new { success = true, message = "Berhasil menghapus role." });
        }
    }
}
