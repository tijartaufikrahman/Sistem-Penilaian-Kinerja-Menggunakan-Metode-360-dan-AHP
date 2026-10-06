using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestAPISistemPenilaian.Interfaces;
using RestAPISistemPenilaian.Models.DTOs;

namespace RestAPISistemPenilaian.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthRepository authRepo, ILogger<AuthController> logger)
        {
            _authRepo = authRepo;
            _logger = logger;
        }

        [HttpPost("login")]
        [HttpPost("/api/users/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                return BadRequest(new { success = false, message = "Email dan password wajib diisi." });

            var loginResult = await _authRepo.LoginAsync(model.Email, model.Password, model.RoleId);
            if (loginResult == null)
            {
                return Unauthorized(new { success = false, message = "Email atau password salah. Silakan coba lagi." });
            }

            _logger.LogInformation("User {Email} berhasil login. JWT Token berhasil digenerate.", model.Email);

            return Ok(new
            {
                success = true,
                message = "Login berhasil.",
                token = loginResult.Token,
                user = new UserAuthDto
                {
                    Id = loginResult.Id,
                    NamaLengkap = loginResult.NamaLengkap,
                    Email = loginResult.Email,
                    RoleId = loginResult.RoleId,
                    RoleName = loginResult.RoleName
                }
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains('@'))
                return BadRequest(new { success = false, message = "Format email tidak valid." });

            var result = await _authRepo.ForgotPasswordAsync(model.Email.Trim());
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message, error = result.Error });
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ForgotPasswordRequestDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains('@'))
                return BadRequest(new { success = false, message = "Format email tidak valid." });

            var result = await _authRepo.ForgotPasswordAsync(model.Email.Trim());
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message, error = result.Error });
            }

            return Ok(new
            {
                success = true,
                message = "Kode OTP baru berhasil dikirim ke email Anda."
            });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Otp))
                return BadRequest(new { success = false, message = "Email dan kode OTP wajib diisi." });

            var result = await _authRepo.VerifyOtpAsync(model.Email.Trim(), model.Otp.Trim());
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto model)
        {
            if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 6)
                return BadRequest(new { success = false, message = "Password minimal 6 karakter." });

            if (!string.IsNullOrEmpty(model.ConfirmPassword) && model.NewPassword != model.ConfirmPassword)
                return BadRequest(new { success = false, message = "Password dan konfirmasi password tidak cocok." });

            // Jika menggunakan OTP (email + otp)
            if (!string.IsNullOrWhiteSpace(model.Email) && !string.IsNullOrWhiteSpace(model.Otp))
            {
                var otpResult = await _authRepo.ResetPasswordWithOtpAsync(model.Email.Trim(), model.Otp.Trim(), model.NewPassword);
                if (!otpResult.Success)
                {
                    return BadRequest(new { success = false, message = otpResult.Message });
                }

                return Ok(new { success = true, message = otpResult.Message });
            }

            // Jika menggunakan Token biasa (link)
            if (!string.IsNullOrWhiteSpace(model.Token))
            {
                var tokenResult = await _authRepo.ResetPasswordAsync(model.Token, model.NewPassword);
                if (!tokenResult)
                {
                    return BadRequest(new { success = false, message = "Link reset password tidak valid atau telah kedaluwarsa." });
                }

                return Ok(new { success = true, message = "Password berhasil diubah! Silakan login kembali." });
            }

            return BadRequest(new { success = false, message = "Data verifikasi reset password tidak lengkap." });
        }

        [HttpGet("validate-token")]
        public async Task<IActionResult> ValidateToken([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { success = false, valid = false, message = "Token tidak boleh kosong." });

            var isValid = await _authRepo.ValidateResetTokenAsync(token);
            if (!isValid)
            {
                return Ok(new
                {
                    success = false,
                    valid = false,
                    message = "Link reset password tidak valid atau telah kedaluwarsa."
                });
            }

            return Ok(new { success = true, valid = true, message = "Token valid." });
        }
    }
}
