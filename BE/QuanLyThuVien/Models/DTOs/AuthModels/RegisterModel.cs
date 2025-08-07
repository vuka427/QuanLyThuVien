using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Models.DTOs.AuthModels
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Tên tài khoản không được bỏ trống")]
        [MaxLength(256, ErrorMessage = "Tên tài khoản có tối đa 256 ký tự"), MinLength(3, ErrorMessage = "Tên tài khoản tối thiểu 3 ký tự")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Họ tên không được bỏ trống")]
        [MaxLength(256, ErrorMessage = "Họ và tên có tối đa 256 ký tự"), MinLength(3, ErrorMessage = "họ tên tối thiểu 3 ký tự")]
        public string? FullName { get; set; }

        [EmailAddress(ErrorMessage = "Định dạng email không đúng !")]
        [Required(ErrorMessage = "Email không được bỏ trống")]
        [MaxLength(256, ErrorMessage = "Email có tối đa 256 ký tự"), MinLength(1, ErrorMessage = "Email tối thiểu 256 ký tự")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được bỏ trống")]
        [MaxLength(50, ErrorMessage = "mật khẩu có tối đa 50 ký tự"), MinLength(6, ErrorMessage = "Email tối thiểu 6 ký tự")]
        public string? Password { get; set; }
    }
}
