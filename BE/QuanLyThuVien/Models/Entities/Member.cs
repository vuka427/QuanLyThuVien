using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Models.Entities
{
    public class Member
    {
        [Key]
        public int MemberId { get; set; }

        [Required]
        [StringLength(20)]
        public string MemberCode { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(15)]
        public string Phone { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        public DateTime DateOfBirth { get; set; }

        public DateTime MembershipDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
    }
}
