namespace RestAPISistemPenilaian.Models.DTOs
{
    public class MenuRoleDto
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string NamaMenu { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int Urutan { get; set; }
    }

    public class CreateMenuRoleDto
    {
        public int RoleId { get; set; }
        public string NamaMenu { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string Url { get; set; } = string.Empty;
        public int Urutan { get; set; }
    }

    public class EditMenuRoleDto
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string NamaMenu { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string Url { get; set; } = string.Empty;
        public int Urutan { get; set; }
    }
}
