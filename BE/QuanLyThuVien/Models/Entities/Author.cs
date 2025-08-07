using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Models.Entities
{
    public class Author
    {
        [Key]
        public int AuthorId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(100)]
        public string PenName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(50)]
        public string Nationality { get; set; }

        [StringLength(500)]
        public string Biography { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    }
}
