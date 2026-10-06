namespace RestAPISistemPenilaian.Models.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string KodeKaryawan { get; set; } = string.Empty;
        public string NamaLengkap { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class CreateUserDto
    {
        public string KodeKaryawan { get; set; } = string.Empty;
        public string NamaLengkap { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? ConfirmPassword { get; set; }
        public int RoleId { get; set; }
        public bool SendEmail { get; set; } = false;
        public string? LoginUrl { get; set; }
    }

    public class EditUserDto
    {
        public int Id { get; set; }
        public string KodeKaryawan { get; set; } = string.Empty;
        public string NamaLengkap { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
    }
}
