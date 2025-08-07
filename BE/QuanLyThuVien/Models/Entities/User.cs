using Microsoft.AspNetCore.Identity;

namespace QuanLyThuVien.Models.Entities
{
    public class User : IdentityUser<Guid>
    {
        public string FullName { get; set; } = null!;

    }
}
