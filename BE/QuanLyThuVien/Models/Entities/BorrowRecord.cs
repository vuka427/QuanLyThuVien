using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Models.Entities
{
    public class BorrowRecord
    {
        [Key]
        public int BorrowId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [Required]
        public int BookId { get; set; }

        public DateTime BorrowDate { get; set; } = DateTime.Now;

        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        public bool IsReturned { get; set; } = false;

        [Column(TypeName = "decimal(10,2)")]
        public decimal FineAmount { get; set; } = 0;

        public bool FinePaid { get; set; } = false;

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        [ForeignKey("MemberId")]
        public virtual Member Member { get; set; }

        [ForeignKey("BookId")]
        public virtual Book Book { get; set; }
    }
}
