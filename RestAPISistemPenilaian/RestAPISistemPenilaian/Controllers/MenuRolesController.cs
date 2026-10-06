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
    public class MenuRolesController : ControllerBase
    {
        private readonly IMenuRoleRepository _menuRoleRepo;
        private readonly ILogger<MenuRolesController> _logger;

        public MenuRolesController(IMenuRoleRepository menuRoleRepo, ILogger<MenuRolesController> logger)
        {
            _menuRoleRepo = menuRoleRepo;
            _logger = logger;
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var count = await _menuRoleRepo.CountAsync();
            return Ok(new { count });
        }

        [HttpGet]
        [HttpGet("/api/admin/GetMenuRoles")]
        public async Task<IActionResult> GetAll()
        {
            var menus = await _menuRoleRepo.GetAllAsync();
            var result = menus.Select(m => new {
                m.Id,
                m.RoleId,
                RoleName = m.Role != null ? m.Role.NamaRole : "-",
                m.NamaMenu,
                Icon = m.Icon ?? "",
                m.Url,
                m.Urutan
            });

            return Ok(result);
        }

        [HttpGet("role/{roleId}")]
        [HttpGet("/api/admin/menuroles/role/{roleId}")]
        public async Task<IActionResult> GetByRole(int roleId)
        {
            var menus = await _menuRoleRepo.GetByRoleIdAsync(roleId);
            var result = menus.Select(m => new {
                m.Id,
                m.RoleId,
                m.NamaMenu,
                Icon = m.Icon ?? "",
                m.Url,
                m.Urutan
            });

            return Ok(result);
        }

        [HttpPost]
        [HttpPost("/api/admin/CreateMenuRole")]
        public async Task<IActionResult> Create([FromForm] CreateMenuRoleDto model)
        {
            if (string.IsNullOrWhiteSpace(model.NamaMenu))
                return Ok(new { success = false, message = "Nama menu wajib diisi." });
            if (string.IsNullOrWhiteSpace(model.Url))
                return Ok(new { success = false, message = "URL wajib diisi." });
            if (model.RoleId <= 0)
                return Ok(new { success = false, message = "Pilih role terlebih dahulu." });

            var menuRole = new MenuRole
            {
                RoleId = model.RoleId,
                NamaMenu = model.NamaMenu.Trim(),
                Icon = model.Icon?.Trim(),
                Url = model.Url.Trim(),
                Urutan = model.Urutan
            };

            await _menuRoleRepo.AddAsync(menuRole);
            await _menuRoleRepo.SaveChangesAsync();

            return Ok(new { success = true, message = "Berhasil menambah menu role baru." });
        }

        [HttpPost("{id}")]
        [HttpPut("{id}")]
        [HttpPost("/api/admin/EditMenuRole/{id?}")]
        public async Task<IActionResult> Edit([FromRoute] int? id, [FromForm] EditMenuRoleDto model)
        {
            var targetId = id.HasValue && id.Value > 0 ? id.Value : model.Id;

            if (targetId <= 0)
                return Ok(new { success = false, message = "ID tidak valid." });

            var current = await _menuRoleRepo.GetByIdAsync(targetId);
            if (current == null)
                return Ok(new { success = false, message = "Menu role tidak ditemukan." });

            if (string.IsNullOrWhiteSpace(model.NamaMenu))
                return Ok(new { success = false, message = "Nama menu wajib diisi." });
            if (string.IsNullOrWhiteSpace(model.Url))
                return Ok(new { success = false, message = "URL wajib diisi." });
            if (model.RoleId <= 0)
                return Ok(new { success = false, message = "Pilih role terlebih dahulu." });

            current.RoleId = model.RoleId;
            current.NamaMenu = model.NamaMenu.Trim();
            current.Icon = model.Icon?.Trim();
            current.Url = model.Url.Trim();
            current.Urutan = model.Urutan;

            await _menuRoleRepo.UpdateAsync(current);
            await _menuRoleRepo.SaveChangesAsync();

            return Ok(new { success = true, message = "Berhasil memperbarui menu role." });
        }

        [HttpPost("delete")]
        [HttpDelete("{id}")]
        [HttpPost("/api/admin/DeleteMenuRole/{id?}")]
        public async Task<IActionResult> Delete([FromRoute] int? id, [FromForm] DeleteRequestDto? formDto)
        {
            var targetId = id.HasValue && id.Value > 0 ? id.Value : (formDto?.Id ?? 0);
            if (targetId <= 0)
                return Ok(new { success = false, message = "ID tidak valid." });

            var menuRole = await _menuRoleRepo.GetByIdAsync(targetId);
            if (menuRole == null)
                return Ok(new { success = false, message = "Menu role tidak ditemukan." });

            await _menuRoleRepo.DeleteAsync(targetId);
            await _menuRoleRepo.SaveChangesAsync();

            return Ok(new { success = true, message = "Berhasil menghapus menu role." });
        }
    }
}
