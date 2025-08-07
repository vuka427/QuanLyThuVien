using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Models.Entities
{
    public class BookAuthor
    {
        [Key]
        public int BookAuthorId { get; set; }

        [Required]
        public int BookId { get; set; }

        [Required]
        public int AuthorId { get; set; }

        public bool IsPrimaryAuthor { get; set; } = false;

        // Navigation properties
        [ForeignKey("BookId")]
        public virtual Book Book { get; set; }

        [ForeignKey("AuthorId")]
        public virtual Author Author { get; set; }
    }
}
