using RestAPISistemPenilaian.Models.DTOs;
using RestAPISistemPenilaian.Models.Entities;

namespace RestAPISistemPenilaian.Interfaces
{
    public interface IAuthRepository
    {
        Task<LoginResponseDto?> LoginAsync(string email, string password, int? roleId = null);
        Task<AuthResultDto> ForgotPasswordAsync(string email);
        Task<AuthResultDto> VerifyOtpAsync(string email, string otp);
        Task<AuthResultDto> ResetPasswordWithOtpAsync(string email, string otp, string newPassword);
        Task<bool> ValidateResetTokenAsync(string token);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }
}
