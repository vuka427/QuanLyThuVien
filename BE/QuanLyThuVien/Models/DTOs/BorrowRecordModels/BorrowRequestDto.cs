namespace QuanLyThuVien.Models.DTOs.BorrowRecordModels
{
    public class BorrowRequestDto
    {
        public int MemberId { get; set; }
        public int BookId { get; set; }
        public int? BorrowDays { get; set; } = 14;
        public string? Notes { get; set; }
    }
}
