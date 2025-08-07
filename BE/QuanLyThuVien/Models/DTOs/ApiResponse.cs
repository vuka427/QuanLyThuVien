namespace QuanLyThuVien.Models.DTOs
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public string? Message { get; set; }
        public int StatusCode { get; set; }
    }
}