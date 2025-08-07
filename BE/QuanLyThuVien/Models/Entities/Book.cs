using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Models.Entities
{
    public class Book
    {
        [Key]
        public int BookId { get; set; }

        [Required]
        [StringLength(13)]
        public string ISBN { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(100)]
        public string Author { get; set; }

        [StringLength(50)]
        public string Publisher { get; set; }

        public DateTime PublishedDate { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int TotalCopies { get; set; }

        public int AvailableCopies { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }
        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
        public virtual ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
        // Computed property for display
        [NotMapped]
        public string AuthorsDisplay => string.Join(", ", BookAuthors?.Select(ba => ba.Author?.FullName) ?? new List<string>());
    }
}
