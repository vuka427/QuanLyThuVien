namespace QuanLyThuVien.Models.DTOs.BookModels
{
    public class BookCreateDto
    {
        public string ISBN { get; set; }
        public string Title { get; set; }
        public string Publisher { get; set; }
        public DateTime PublishedDate { get; set; }
        public int CategoryId { get; set; }
        public int TotalCopies { get; set; }
        public List<int> AuthorIds { get; set; } = new List<int>();
    }
}
