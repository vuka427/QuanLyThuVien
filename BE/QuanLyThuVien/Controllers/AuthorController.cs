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
    public class AuthorController : ControllerBase
    {
        private readonly AppDBContext _context;

        public AuthorController(AppDBContext context)
        {
            _context = context;
        }

        // GET: api/Author
        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetAuthors()
        {
            var authors = await _context.Author
                .Where(a => a.IsActive)
                .OrderBy(a => a.FullName)
                .ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = authors,
                Message = "Lấy danh sách tác giả thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Author/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse>> GetAuthor(int id)
        {
            var author = await _context.Author
                .Include(a => a.BookAuthors)
                .ThenInclude(ba => ba.Book)
                .ThenInclude(b => b.Category)
                .FirstOrDefaultAsync(a => a.AuthorId == id);

            if (author == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Tác giả không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            return Ok(new ApiResponse
            {
                Success = true,
                Data = author,
                Message = "Lấy thông tin tác giả thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Author/5/books
        [HttpGet("{id}/books")]
        public async Task<ActionResult<ApiResponse>> GetBooksByAuthor(int id)
        {
            var books = await _context.BookAuthor
                .Include(ba => ba.Book)
                .ThenInclude(b => b.Category)
                .Include(ba => ba.Book)
                .ThenInclude(b => b.BookAuthors)
                .ThenInclude(ba2 => ba2.Author)
                .Where(ba => ba.AuthorId == id)
                .Select(ba => ba.Book)
                .ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = books,
                Message = "Lấy danh sách sách của tác giả thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Author/search?term=martin
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse>> SearchAuthors(string term)
        {
            var authors = string.IsNullOrEmpty(term)
                ? await _context.Author
                    .Where(a => a.IsActive)
                    .OrderBy(a => a.FullName)
                    .ToListAsync()
                : await _context.Author
                    .Where(a => a.IsActive &&
                        (a.FullName.Contains(term) ||
                         a.PenName.Contains(term) ||
                         a.Nationality.Contains(term)))
                    .OrderBy(a => a.FullName)
                    .ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = authors,
                Message = "Tìm kiếm tác giả thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // POST: api/Author
        [HttpPost]
        public async Task<ActionResult<ApiResponse>> PostAuthor(Author author)
        {
            try
            {
                _context.Author.Add(author);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetAuthor", new { id = author.AuthorId }, new ApiResponse
                {
                    Success = true,
                    Data = author,
                    Message = "Thêm tác giả thành công",
                    StatusCode = StatusCodes.Status201Created
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"Lỗi khi thêm tác giả: {ex.Message}",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }
        }

        // PUT: api/Author/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> PutAuthor(int id, Author author)
        {
            if (id != author.AuthorId)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "ID không khớp",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            _context.Entry(author).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = author,
                    Message = "Cập nhật tác giả thành công",
                    StatusCode = StatusCodes.Status200OK
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthorExists(id))
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Tác giả không tồn tại",
                        StatusCode = StatusCodes.Status404NotFound
                    });
                }
                throw;
            }
        }

        // DELETE: api/Author/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteAuthor(int id)
        {
            var author = await _context.Author.FindAsync(id);
            if (author == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Tác giả không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            // Check if author has books
            var hasBooks = await _context.BookAuthor.AnyAsync(ba => ba.AuthorId == id);
            if (hasBooks)
            {
                // Soft delete
                author.IsActive = false;
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = author,
                    Message = "Tác giả đã được ẩn do có sách liên quan",
                    StatusCode = StatusCodes.Status200OK
                });
            }
            else
            {
                // Hard delete
                _context.Author.Remove(author);
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Tác giả đã được xóa",
                    StatusCode = StatusCodes.Status200OK
                });
            }
        }

        private bool AuthorExists(int id)
        {
            return _context.Author.Any(e => e.AuthorId == id);
        }
    }
}
