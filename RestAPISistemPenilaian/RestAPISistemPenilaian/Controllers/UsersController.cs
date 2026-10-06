using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestAPISistemPenilaian.Data;
using RestAPISistemPenilaian.Interfaces;
using RestAPISistemPenilaian.Models.DTOs;
using RestAPISistemPenilaian.Models.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace RestAPISistemPenilaian.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly AppDbContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly IConfiguration _config;

        public UsersController(
            IUserRepository userRepo,
            AppDbContext context,
            ILogger<UsersController> logger,
            IConfiguration config)
        {
            _userRepo = userRepo;
            _context = context;
            _logger = logger;
            _config = config;
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var count = await _userRepo.CountAsync();
            return Ok(new { count });
        }

        [HttpGet]
        [HttpGet("/api/admin/GetUsers")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userRepo.GetAllAsync();
            var result = users
                .OrderBy(u => u.NamaLengkap)
                .Select(u => new {
                    u.Id,
                    u.KodeKaryawan,
                    u.NamaLengkap,
                    u.Email,
                    u.RoleId,
                    RoleName = u.Role != null ? u.Role.NamaRole : "-",
                    u.IsActive,
                    CreatedAt = u.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                });

            return Ok(result);
        }

        [HttpGet("karyawan")]
        public async Task<IActionResult> GetKaryawanOnly()
        {
            var users = await _userRepo.GetAllAsync();
            var result = users
                .Where(u => u.RoleId == 2 && u.IsActive)
                .OrderBy(u => u.NamaLengkap)
                .Select(u => new {
                    u.Id,
                    u.KodeKaryawan,
                    u.NamaLengkap,
                    u.Email,
                    u.RoleId,
                    RoleName = u.Role != null ? u.Role.NamaRole : "Karyawan"
                });

            return Ok(result);
        }

        [HttpGet("{id}")]
        [HttpGet("/api/admin/GetUser/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var u = await _userRepo.GetByIdAsync(id);
            if (u == null) return NotFound(new { success = false, message = "User tidak ditemukan" });

            return Ok(new {
                u.Id,
                u.KodeKaryawan,
                u.NamaLengkap,
                u.Email,
                u.RoleId,
                RoleName = u.Role?.NamaRole ?? "-",
                u.IsActive,
                CreatedAt = u.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            });
        }

        [HttpPost]
        [HttpPost("/api/admin/CreateUser")]
        public async Task<IActionResult> Create([FromForm] CreateUserDto model)
        {
            if (string.IsNullOrWhiteSpace(model.NamaLengkap))
                return Ok(new { success = false, message = "Nama lengkap wajib diisi." });
            if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains('@'))
                return Ok(new { success = false, message = "Email tidak valid." });
            if (string.IsNullOrWhiteSpace(model.PasswordHash) || model.PasswordHash.Length < 6)
                return Ok(new { success = false, message = "Password minimal 6 karakter." });
            if (!string.IsNullOrEmpty(model.ConfirmPassword) && model.ConfirmPassword != model.PasswordHash)
                return Ok(new { success = false, message = "Password dan konfirmasi tidak cocok." });
            if (model.RoleId <= 0)
                return Ok(new { success = false, message = "Pilih role terlebih dahulu." });

            var existing = await _userRepo.GetByEmailAsync(model.Email);
            if (existing != null)
                return Ok(new { success = false, message = "Email sudah terdaftar." });

            string kode = !string.IsNullOrWhiteSpace(model.KodeKaryawan)
                ? model.KodeKaryawan.Trim()
                : (model.RoleId == 1 ? $"HRD-{Guid.NewGuid().ToString("N")[..6].ToUpper()}" : $"KRY-{Guid.NewGuid().ToString("N")[..6].ToUpper()}");

            string plainPassword = model.PasswordHash;

            var user = new User
            {
                KodeKaryawan = kode,
                NamaLengkap = model.NamaLengkap.Trim(),
                Email = model.Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                RoleId = model.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();

            bool emailSent = false;
            string? emailError = null;

            if (model.SendEmail)
            {
                try
                {
                    string roleName = model.RoleId == 1 ? "HRD (Administrator)" : "Karyawan";
                    var roleObj = await _context.Roles.FindAsync(model.RoleId);
                    if (roleObj != null && !string.IsNullOrWhiteSpace(roleObj.NamaRole))
                    {
                        roleName = roleObj.NamaRole;
                    }

                    string loginUrl = !string.IsNullOrWhiteSpace(model.LoginUrl)
                        ? model.LoginUrl.Trim()
                        : "https://localhost:9009/Account/Login";

                    await SendAccountCredentialsEmailAsync(
                        user.Email,
                        user.NamaLengkap,
                        user.KodeKaryawan,
                        plainPassword,
                        roleName,
                        loginUrl
                    );
                    emailSent = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gagal mengirim email kredensial ke {Email}", model.Email);
                    emailError = ex.Message;
                }
            }

            if (model.SendEmail && !emailSent)
            {
                return Ok(new { 
                    success = true, 
                    message = $"Karyawan baru berhasil ditambahkan, namun pengiriman email informasi gagal: {emailError}",
                    emailSent = false
                });
            }

            return Ok(new { 
                success = true, 
                message = model.SendEmail 
                    ? $"Karyawan baru berhasil ditambahkan dan informasi akun telah dikirim ke email {model.Email}." 
                    : "Berhasil menambah user baru.",
                emailSent = model.SendEmail
            });
        }

        private async Task SendAccountCredentialsEmailAsync(string toEmail, string toName, string kodeKaryawan, string plainPassword, string roleName, string loginUrl)
        {
            var host = _config["EmailSettings:Host"] ?? _config["Smtp:Host"] ?? "smtp.gmail.com";
            var port = int.Parse(_config["EmailSettings:Port"] ?? _config["Smtp:Port"] ?? "587");
            var senderEmail = _config["EmailSettings:Email"] ?? _config["Smtp:Username"] ?? "";
            var senderPassword = _config["EmailSettings:Password"] ?? _config["Smtp:Password"] ?? "";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Sistem Penilaian Kinerja", senderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = "Informasi Akun & Akses Login Sistem Penilaian Kinerja";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
<!DOCTYPE html>
<html lang='id'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, Helvetica, Arial, sans-serif; background-color: #f1f5f9; margin: 0; padding: 24px; color: #1e293b; }}
        .container {{ max-width: 520px; margin: 0 auto; background: #ffffff; border-radius: 14px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.06); border: 1px solid #e2e8f0; }}
        .header {{ background: linear-gradient(135deg, #0a101d 0%, #1e293b 60%, #0d9488 100%); padding: 32px 24px; text-align: center; color: #ffffff; }}
        .header h1 {{ margin: 0; font-size: 20px; font-weight: 700; letter-spacing: -0.3px; }}
        .header p {{ margin: 6px 0 0; font-size: 12.5px; color: #99f6e4; font-weight: 500; }}
        .content {{ padding: 28px 24px; }}
        .greeting {{ font-size: 15px; margin-bottom: 12px; color: #0f172a; }}
        .intro {{ font-size: 13.5px; line-height: 1.6; color: #475569; margin-bottom: 20px; }}
        .cred-box {{ background: #f8fafc; border: 1px solid #cbd5e1; border-radius: 10px; padding: 18px 20px; margin-bottom: 22px; }}
        .cred-row {{ display: flex; justify-content: space-between; align-items: center; padding: 8px 0; border-bottom: 1px solid #e2e8f0; font-size: 13px; }}
        .cred-row:last-child {{ border-bottom: none; }}
        .cred-label {{ color: #64748b; font-weight: 500; }}
        .cred-val {{ font-weight: 700; color: #0f172a; text-align: right; }}
        .cred-badge {{ background: #f0fdfa; color: #0f766e; border: 1px solid #99f6e4; padding: 2px 8px; border-radius: 6px; font-weight: 600; font-size: 12px; }}
        .btn-login {{ display: block; text-align: center; background: #0d9488; color: #ffffff !important; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 14px; margin: 24px 0 16px 0; box-shadow: 0 2px 8px rgba(13,148,136,0.3); }}
        .url-hint {{ font-size: 11.5px; color: #64748b; word-break: break-all; margin-bottom: 20px; line-height: 1.5; }}
        .notice-box {{ background: #fffbeb; border-left: 4px solid #f59e0b; padding: 12px 14px; border-radius: 6px; font-size: 12px; color: #92400e; line-height: 1.5; }}
        .footer {{ background: #f8fafc; padding: 16px; font-size: 11.5px; color: #94a3b8; text-align: center; border-top: 1px solid #e2e8f0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Sistem Penilaian Kinerja</h1>
            <p>Evaluasi 360° &amp; Pembobotan Metode AHP</p>
        </div>
        <div class='content'>
            <div class='greeting'>Halo, <strong>{toName}</strong></div>
            <p class='intro'>
                Akun Anda telah berhasil dibuat di <strong>Sistem Penilaian Kinerja</strong>. Anda kini dapat masuk ke dalam portal menggunakan informasi kredensial login berikut:
            </p>
            
            <div class='cred-box'>
                <div class='cred-row'>
                    <span class='cred-label'>Kode Karyawan:</span>
                    <span class='cred-val'>{kodeKaryawan}</span>
                </div>
                <div class='cred-row'>
                    <span class='cred-label'>Peran (Role):</span>
                    <span class='cred-val'><span class='cred-badge'>{roleName}</span></span>
                </div>
                <div class='cred-row'>
                    <span class='cred-label'>Email Login:</span>
                    <span class='cred-val' style='font-family:monospace; color:#0f766e;'>{toEmail}</span>
                </div>
                <div class='cred-row'>
                    <span class='cred-label'>Kata Sandi (Password):</span>
                    <span class='cred-val' style='font-family:monospace; font-size:14px; color:#b91c1c; background:#fef2f2; padding:3px 8px; border-radius:4px;'>{plainPassword}</span>
                </div>
            </div>

            <a href='{loginUrl}' class='btn-login' target='_blank'>Masuk ke Sistem Penilaian &rarr;</a>
            
            <div class='url-hint'>
                Atau salin tautan berikut ke peramban (browser) Anda:<br>
                <a href='{loginUrl}' style='color:#0d9488;'>{loginUrl}</a>
            </div>

            <div class='notice-box'>
                <strong>Pemberitahuan Keamanan:</strong><br>
                Demi menjaga keamanan akun dan objektivitas evaluasi penilaian kinerja, Anda disarankan untuk segera memperbarui kata sandi Anda setelah berhasil masuk pertama kali pada menu pengaturan profil.
            </div>
        </div>
        <div class='footer'>
            &copy; 2026 Sistem Penilaian Kinerja. Email ini dibuat secara otomatis oleh sistem, mohon tidak membalas email ini.
        </div>
    </div>
</body>
</html>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, senderPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        [HttpPost("{id}")]
        [HttpPut("{id}")]
        [HttpPost("/api/admin/EditUser/{id?}")]
        public async Task<IActionResult> Edit([FromRoute] int? id, [FromForm] EditUserDto model)
        {
            var targetId = id.HasValue && id.Value > 0 ? id.Value : model.Id;

            if (targetId <= 0)
                return Ok(new { success = false, message = "ID tidak valid." });

            var current = await _userRepo.GetByIdAsync(targetId);
            if (current == null)
                return Ok(new { success = false, message = "User tidak ditemukan." });

            if (string.IsNullOrWhiteSpace(model.NamaLengkap))
                return Ok(new { success = false, message = "Nama lengkap wajib diisi." });
            if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains('@'))
                return Ok(new { success = false, message = "Email tidak valid." });

            if (current.Email != model.Email)
            {
                var existing = await _userRepo.GetByEmailAsync(model.Email);
                if (existing != null && existing.Id != targetId)
                    return Ok(new { success = false, message = "Email sudah digunakan user lain." });
            }

            if (!string.IsNullOrWhiteSpace(model.KodeKaryawan))
            {
                current.KodeKaryawan = model.KodeKaryawan.Trim();
            }

            current.NamaLengkap = model.NamaLengkap.Trim();
            current.Email = model.Email.Trim();
            current.RoleId = model.RoleId;
            current.IsActive = model.IsActive;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                if (model.Password.Length < 6)
                    return Ok(new { success = false, message = "Password baru minimal 6 karakter." });
                current.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            }

            await _userRepo.UpdateAsync(current);
            await _userRepo.SaveChangesAsync();

            return Ok(new { success = true, message = "Data user berhasil diperbarui." });
        }

        [HttpDelete("{id}")]
        [HttpPost("/api/admin/DeleteUser/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(id);
                if (user == null)
                    return Ok(new { success = false, message = "User tidak ditemukan." });

                // Cek apakah user memiliki riwayat di Penilaian360 atau HasilPenilaianSistem
                var hasPenilaian = await _context.Penilaian360s.AnyAsync(p => p.UserId == id || p.PenilaiUserId == id);
                var hasHasil = await _context.HasilPenilaianSistems.AnyAsync(h => h.UserId == id);

                if (hasPenilaian || hasHasil)
                {
                    // Demi integritas data evaluasi multi-rater 360 dan kalkulasi AHP, lakukan Soft Delete
                    user.IsActive = false;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userRepo.UpdateAsync(user);
                    await _userRepo.SaveChangesAsync();

                    return Ok(new { 
                        success = true, 
                        message = "Karyawan ini memiliki riwayat evaluasi 360°/hasil SPK. Status akun berhasil dinonaktifkan (Soft Delete) demi integritas data." 
                    });
                }

                await _userRepo.DeleteAsync(id);
                await _userRepo.SaveChangesAsync();

                return Ok(new { success = true, message = "User berhasil dihapus secara permanen." });
            }
            catch (DbUpdateException)
            {
                try
                {
                    var u = await _userRepo.GetByIdAsync(id);
                    if (u != null)
                    {
                        u.IsActive = false;
                        u.UpdatedAt = DateTime.UtcNow;
                        await _userRepo.UpdateAsync(u);
                        await _userRepo.SaveChangesAsync();
                        return Ok(new { 
                            success = true, 
                            message = "Karyawan dinonaktifkan karena masih memiliki keterkaitan data riwayat pada sistem." 
                        });
                    }
                }
                catch { }

                return Ok(new { success = false, message = "Karyawan tidak dapat dihapus karena masih terkait data lain pada sistem." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat menghapus user {Id}", id);
                return BadRequest(new { success = false, message = "Gagal memproses penghapusan user: " + ex.Message });
            }
        }
    }
}
