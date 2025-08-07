using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.AppContext;
using QuanLyThuVien.Data;
using QuanLyThuVien.Models.DTOs;
using QuanLyThuVien.Models.Entities;

namespace QuanLyThuVien.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = RoleName.Admin)]
    public class CategoryController : ControllerBase
    {
        private readonly AppDBContext _context;

        public CategoryController(AppDBContext context)
        {
            _context = context;
        }

        // GET: api/Category
        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetCategories()
        {
            var categories = await _context.Category
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = categories,
                Message = "Lấy danh sách danh mục thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Category/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse>> GetCategory(int id)
        {
            var category = await _context.Category
                .Include(c => c.Books.Where(b => b.AvailableCopies > 0))
                .ThenInclude(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Danh mục không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            return Ok(new ApiResponse
            {
                Success = true,
                Data = category,
                Message = "Lấy thông tin danh mục thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Category/5/books
        [HttpGet("{id}/books")]
        public async Task<ActionResult<ApiResponse>> GetBooksByCategory(int id)
        {
            var books = await _context.Book
                .Include(b => b.Category)
                .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .Where(b => b.CategoryId == id)
                .OrderBy(b => b.Title)
                .ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = books,
                Message = "Lấy danh sách sách theo danh mục thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Category/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse>> GetCategoryStatistics()
        {
            var stats = await _context.Book
                .Include(b => b.Category)
                .GroupBy(b => b.Category.CategoryName)
                .Select(g => new { CategoryName = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CategoryName, x => x.Count);

            return Ok(new ApiResponse
            {
                Success = true,
                Data = stats,
                Message = "Lấy thống kê danh mục thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Category/popular
        [HttpGet("popular")]
        public async Task<ActionResult<ApiResponse>> GetPopularCategories()
        {
            var popular = await _context.BorrowRecord
                .Include(br => br.Book)
                .ThenInclude(b => b.Category)
                .GroupBy(br => br.Book.Category)
                .Select(g => new
                {
                    Category = g.Key.CategoryName,
                    BorrowCount = g.Count(),
                    CategoryId = g.Key.CategoryId
                })
                .OrderByDescending(x => x.BorrowCount)
                .Take(5)
                .ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = popular,
                Message = "Lấy danh sách danh mục phổ biến thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // POST: api/Category
        [HttpPost]
        public async Task<ActionResult<ApiResponse>> PostCategory(Category category)
        {
            try
            {
                _context.Category.Add(category);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetCategory", new { id = category.CategoryId }, new ApiResponse
                {
                    Success = true,
                    Data = category,
                    Message = "Thêm danh mục thành công",
                    StatusCode = StatusCodes.Status201Created
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"Lỗi khi thêm danh mục: {ex.Message}",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }
        }

        // PUT: api/Category/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> PutCategory(int id, Category category)
        {
            if (id != category.CategoryId)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "ID không khớp",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = category,
                    Message = "Cập nhật danh mục thành công",
                    StatusCode = StatusCodes.Status200OK
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Danh mục không tồn tại",
                        StatusCode = StatusCodes.Status404NotFound
                    });
                }
                throw;
            }
        }

        // DELETE: api/Category/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteCategory(int id)
        {
            var category = await _context.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Danh mục không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            // Check if category has books
            var hasBooks = await _context.Book.AnyAsync(b => b.CategoryId == id);
            if (hasBooks)
            {
                // Soft delete
                category.IsActive = false;
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = category,
                    Message = "Danh mục đã được ẩn do có sách liên quan",
                    StatusCode = StatusCodes.Status200OK
                });
            }
            else
            {
                // Hard delete
                _context.Category.Remove(category);
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Danh mục đã được xóa",
                    StatusCode = StatusCodes.Status200OK
                });
            }
        }

        private bool CategoryExists(int id)
        {
            return _context.Category.Any(e => e.CategoryId == id);
        }
    }
}
