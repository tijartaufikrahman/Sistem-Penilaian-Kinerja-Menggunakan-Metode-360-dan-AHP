namespace RestAPISistemPenilaian.Models.DTOs
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string NamaRole { get; set; } = string.Empty;
        public string Deskripsi { get; set; } = string.Empty;
    }

    public class CreateRoleDto
    {
        public string NamaRole { get; set; } = string.Empty;
        public string? Deskripsi { get; set; }
    }

    public class EditRoleDto
    {
        public int Id { get; set; }
        public string NamaRole { get; set; } = string.Empty;
        public string? Deskripsi { get; set; }
    }
}
