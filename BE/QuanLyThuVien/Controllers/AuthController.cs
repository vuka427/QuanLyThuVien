using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using QuanLyThuVien.Models.DTOs;
using QuanLyThuVien.Models.DTOs.AuthModels;
using QuanLyThuVien.Models.Entities;
using System.Diagnostics.Metrics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using QuanLyThuVien.Data;

namespace QuanLyThuVien.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<User> userManager, RoleManager<Role> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<ApiResponse> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim("id",  user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = GetToken(authClaims);

                var roles = await _userManager.GetRolesAsync(user);

                return new ApiResponse
                {
                    Success = true,
                    Data = new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(token),
                        expiration = token.ValidTo,
                        user = new { user.Id, user.UserName, user.FullName, user.Email, user.PhoneNumber, roles },
                    },
                    Message = "Đăng nhập thành công",
                    StatusCode =  StatusCodes.Status200OK
                };


            }
            return new ApiResponse
            {
                Success = false,
                StatusCode = StatusCodes.Status200OK,
                Message = "Đăng nhập thất bại"
            };
        }

        [HttpPost]
        public async Task<ApiResponse> Register([FromBody] RegisterModel model)
        {
            await _roleManager.CreateAsync(new Role
            {
                Id = Guid.NewGuid(),
                Name = RoleName.User
            });

            await _roleManager.CreateAsync(new Role
            {
                Id = Guid.NewGuid(),
                Name = RoleName.Admin
            });



            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
                return new ApiResponse { Success = false, StatusCode = StatusCodes.Status200OK, Message = "Username đã tồn tại" };

            User user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username,
                FullName = model.FullName??""

            };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                //await _roleManager.CreateAsync(new Role { Name = RoleName.User });
                //await _roleManager.CreateAsync(new Role { Name = RoleName.Admin });


                await _userManager.AddToRoleAsync(user, RoleName.User);
                return new ApiResponse { Success = true, StatusCode = StatusCodes.Status200OK, Message = "Đăng ký thành công" };
            }

            return new ApiResponse { Success = false, StatusCode = StatusCodes.Status200OK, Message = "Đăng ký thất bại" };
        }
        
        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(24),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );
            return token;
        }
        
    }
}
