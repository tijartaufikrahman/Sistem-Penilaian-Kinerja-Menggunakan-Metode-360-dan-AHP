using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using RestAPISistemPenilaian.Data;
using RestAPISistemPenilaian.Interfaces;
using RestAPISistemPenilaian.Models.DTOs;
using RestAPISistemPenilaian.Models.Entities;

namespace RestAPISistemPenilaian.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(
            AppDbContext context,
            IConfiguration config,
            ILogger<AuthRepository> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        public async Task<LoginResponseDto?> LoginAsync(string email, string password, int? roleId = null)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .Where(u => u.Email == email && u.IsActive);

            if (roleId.HasValue && roleId.Value > 0)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
            }

            var user = await query.FirstOrDefaultAsync();
            if (user == null || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Format password hash tidak valid untuk user {Email}", email);
                isPasswordValid = false;
            }

            if (!isPasswordValid)
            {
                return null;
            }

            var roleName = user.Role?.NamaRole ?? "Guest";
            var token = GenerateJwtToken(user, roleName);

            return new LoginResponseDto
            {
                Success = true,
                Message = "Login berhasil.",
                Token = token,
                Id = user.Id,
                NamaLengkap = user.NamaLengkap,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = roleName
            };
        }

        public string GenerateJwtToken(User user, string roleName)
        {
            var rawKey = _config["Jwt:Key"] ?? "SistemPenilaian360AHP_SuperSecretSecurityKey_2026_SecureAuthentication!";
            // Memastikan key berukuran minimal 256 bits (32 bytes) untuk HMAC-SHA256
            var keyBytes = Encoding.UTF8.GetBytes(rawKey);
            var safeKeyBytes = keyBytes.Length >= 32 ? keyBytes : SHA256.HashData(keyBytes);
            var securityKey = new SymmetricSecurityKey(safeKeyBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("id", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.NamaLengkap),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("roleId", user.RoleId.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            int expiryDays = int.TryParse(_config["Jwt:ExpiryDays"], out int days) ? days : 1;

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "SistemPenilaianAPI",
                audience: _config["Jwt:Audience"] ?? "SistemPenilaianClient",
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(expiryDays), // Default 1 hari (24 jam)
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<AuthResultDto> ForgotPasswordAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Email tidak terdaftar di sistem."
                };
            }

            // Generate 6-digit OTP
            var otp = Random.Shared.Next(100000, 999999).ToString();
            user.ResetPasswordToken = otp;
            user.ResetPasswordExpiry = DateTime.UtcNow.AddMinutes(15);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("========================================");
            _logger.LogInformation("[RESET PASSWORD OTP] Email: {Email}, Kode OTP: {Otp}", user.Email, otp);
            _logger.LogInformation("========================================");

            try
            {
                await SendOtpEmailAsync(user.Email, user.NamaLengkap, otp);
                return new AuthResultDto
                {
                    Success = true,
                    Message = "Kode OTP berhasil dikirim ke email Anda."
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gagal mengirim email OTP ke {Email}. OTP fallback disertakan dalam response.", user.Email);
                return new AuthResultDto
                {
                    Success = true,
                    Message = $"Kode OTP berhasil digenerate: {otp}"
                };
            }
        }

        public async Task<AuthResultDto> VerifyOtpAsync(string email, string otp)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null || user.ResetPasswordToken != otp || user.ResetPasswordExpiry < DateTime.UtcNow)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Kode OTP tidak valid atau telah kedaluwarsa."
                };
            }

            return new AuthResultDto
            {
                Success = true,
                Message = "Kode OTP valid."
            };
        }

        public async Task<AuthResultDto> ResetPasswordWithOtpAsync(string email, string otp, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null || user.ResetPasswordToken != otp || user.ResetPasswordExpiry < DateTime.UtcNow)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Kode OTP tidak valid atau telah kedaluwarsa."
                };
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return new AuthResultDto
            {
                Success = true,
                Message = "Password berhasil diubah. Silakan login dengan password baru."
            };
        }

        public async Task<bool> ValidateResetTokenAsync(string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetPasswordToken == token);
            if (user == null || user.ResetPasswordExpiry < DateTime.UtcNow)
                return false;

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetPasswordToken == token);
            if (user == null || user.ResetPasswordExpiry < DateTime.UtcNow)
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task SendOtpEmailAsync(string toEmail, string toName, string otp)
        {
            var host = _config["EmailSettings:Host"] ?? _config["Smtp:Host"] ?? "smtp.gmail.com";
            var port = int.Parse(_config["EmailSettings:Port"] ?? _config["Smtp:Port"] ?? "587");
            var senderEmail = _config["EmailSettings:Email"] ?? _config["Smtp:Username"] ?? "";
            var senderPassword = _config["EmailSettings:Password"] ?? _config["Smtp:Password"] ?? "";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Sistem Penilaian Kinerja", senderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = $"{otp} adalah Kode Verifikasi OTP Reset Password Anda";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <!DOCTYPE html>
                <html lang='id'>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f1f5f9; margin: 0; padding: 24px; }}
                        .container {{ max-width: 480px; margin: 0 auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.06); border: 1px solid #e2e8f0; }}
                        .header {{ background: linear-gradient(135deg, #2dd4bf 0%, #0d9488 100%); padding: 32px 24px; text-align: center; color: #ffffff; }}
                        .header h1 {{ margin: 0; font-size: 20px; font-weight: 700; }}
                        .header p {{ margin: 6px 0 0; font-size: 13px; opacity: 0.9; }}
                        .content {{ padding: 32px 28px; text-align: center; color: #334155; }}
                        .greeting {{ font-size: 14px; margin-bottom: 16px; text-align: left; }}
                        .otp-box {{ background: #f0fdfa; border: 2px dashed #0d9488; border-radius: 12px; padding: 18px 24px; font-size: 32px; font-weight: 800; letter-spacing: 8px; color: #0d9488; margin: 20px 0; display: inline-block; font-family: monospace; }}
                        .notice {{ font-size: 12.5px; color: #64748b; line-height: 1.6; margin-top: 16px; }}
                        .footer {{ background: #f8fafc; padding: 16px; font-size: 11.5px; color: #94a3b8; text-align: center; border-top: 1px solid #e2e8f0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Sistem Penilaian Kinerja</h1>
                            <p>Permintaan Verifikasi Reset Password</p>
                        </div>
                        <div class='content'>
                            <div class='greeting'>Halo <strong>{toName}</strong>,</div>
                            <p style='font-size: 13.5px; line-height: 1.6; margin-bottom: 8px;'>Berikut adalah kode OTP untuk memverifikasi akun Anda:</p>
                            <div class='otp-box'>{otp}</div>
                            <div class='notice'>
                                Kode OTP ini berlaku selama <strong>15 menit</strong>.<br>
                                Jangan berikan kode ini kepada siapa pun demi keamanan akun Anda.
                            </div>
                        </div>
                        <div class='footer'>
                            &copy; 2026 Sistem Penilaian Kinerja. Email dikirim secara otomatis.
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
    }
}
