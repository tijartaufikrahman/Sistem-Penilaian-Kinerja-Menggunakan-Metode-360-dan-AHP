namespace RestAPISistemPenilaian.Models.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Sukses")
            => new ApiResponse<T> { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message)
            => new ApiResponse<T> { Success = false, Message = message, Data = default };
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ApiResponse Ok(string message = "Sukses")
            => new ApiResponse { Success = true, Message = message };

        public static ApiResponse Fail(string message)
            => new ApiResponse { Success = false, Message = message };
    }

    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalRoles { get; set; }
        public int TotalMenus { get; set; }
    }

    public class DeleteRequestDto
    {
        public int Id { get; set; }
    }
}
