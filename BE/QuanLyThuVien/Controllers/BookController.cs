using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.AppContext;
using QuanLyThuVien.Data;
using QuanLyThuVien.Models.DTOs;
using QuanLyThuVien.Models.DTOs.BookModels;
using QuanLyThuVien.Models.Entities;

namespace QuanLyThuVien.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = RoleName.Admin)]
    public class BookController : ControllerBase
    {
        private readonly AppDBContext _context;

        public BookController(AppDBContext context)
        {
            _context = context;
        }

        // GET: api/Book
        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetBooks()
        {
            var books = await _context.Book
                .Include(b => b.Category)
                .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .OrderBy(b => b.Title)
                .ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = books,
                Message = "Lấy danh sách sách thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Book/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse>> GetBook(int id)
        {
            var book = await _context.Book
                .Include(b => b.Category)
                .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .FirstOrDefaultAsync(b => b.BookId == id);
            if (book == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Sách không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            return Ok(new ApiResponse
            {
                Success = true,
                Data = book,
                Message = "Lấy thông tin sách thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Book/search?term=clean&categoryId=1&authorId=2
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse>> SearchBooks(
            string term, int? categoryId = null, int? authorId = null)
        {
            var query = _context.Book
                .Include(b => b.Category)
                .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .AsQueryable();

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(b =>
                    b.Title.Contains(term) ||
                    b.ISBN.Contains(term) ||
                    b.BookAuthors.Any(ba => ba.Author.FullName.Contains(term)) ||
                    b.BookAuthors.Any(ba => ba.Author.PenName.Contains(term)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(b => b.CategoryId == categoryId.Value);
            }

            if (authorId.HasValue)
            {
                query = query.Where(b => b.BookAuthors.Any(ba => ba.AuthorId == authorId.Value));
            }

            var books = await query.OrderBy(b => b.Title).ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = books,
                Message = "Tìm kiếm sách thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Book/available
        [HttpGet("available")]
        public async Task<ActionResult<ApiResponse>> GetAvailableBooks()
        {
            var books = await _context.Book
                .Include(b => b.Category)
                .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .Where(b => b.AvailableCopies > 0)
                .OrderBy(b => b.Title)
                .ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = books,
                Message = "Lấy danh sách sách có sẵn thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // POST: api/Book
        [HttpPost]
        public async Task<ActionResult<ApiResponse>> PostBook(BookCreateDto bookDto)
        {
            // Validate category
            var categoryExists = await _context.Category
                .AnyAsync(c => c.CategoryId == bookDto.CategoryId && c.IsActive);
            if (!categoryExists)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Danh mục không tồn tại hoặc không hoạt động",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            // Validate authors
            var validAuthorIds = await _context.Author
                .Where(a => bookDto.AuthorIds.Contains(a.AuthorId) && a.IsActive)
                .Select(a => a.AuthorId)
                .ToListAsync();

            if (validAuthorIds.Count != bookDto.AuthorIds.Count)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Một số tác giả không tồn tại hoặc không hoạt động",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            var book = new Book
            {
                ISBN = bookDto.ISBN,
                Title = bookDto.Title,
                Publisher = bookDto.Publisher,
                PublishedDate = bookDto.PublishedDate,
                CategoryId = bookDto.CategoryId,
                TotalCopies = bookDto.TotalCopies,
                AvailableCopies = bookDto.TotalCopies
            };

            try
            {
                _context.Book.Add(book);
                await _context.SaveChangesAsync();

                // Add author relationships
                for (int i = 0; i < bookDto.AuthorIds.Count; i++)
                {
                    var bookAuthor = new BookAuthor
                    {
                        BookId = book.BookId,
                        AuthorId = bookDto.AuthorIds[i],
                        IsPrimaryAuthor = i == 0
                    };
                    _context.BookAuthor.Add(bookAuthor);
                }

                await _context.SaveChangesAsync();

                // Load full book data
                var createdBook = await _context.Book
                    .Include(b => b.Category)
                    .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                    .FirstOrDefaultAsync(b => b.BookId == book.BookId);

                return CreatedAtAction("GetBook", new { id = book.BookId }, new ApiResponse
                {
                    Success = true,
                    Data = createdBook,
                    Message = "Thêm sách thành công",
                    StatusCode = StatusCodes.Status201Created
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"Lỗi khi thêm sách: {ex.Message}",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }
        }

        // PUT: api/Book/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> PutBook(int id, BookUpdateDto bookDto)
        {
            var book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Sách không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            // Validate category
            var categoryExists = await _context.Category
                .AnyAsync(c => c.CategoryId == bookDto.CategoryId && c.IsActive);
            if (!categoryExists)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Danh mục không tồn tại hoặc không hoạt động",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            // Validate authors
            var validAuthorIds = await _context.Author
                .Where(a => bookDto.AuthorIds.Contains(a.AuthorId) && a.IsActive)
                .Select(a => a.AuthorId)
                .ToListAsync();

            if (validAuthorIds.Count != bookDto.AuthorIds.Count)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Một số tác giả không tồn tại hoặc không hoạt động",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            try
            {
                // Update book properties
                book.ISBN = bookDto.ISBN;
                book.Title = bookDto.Title;
                book.Publisher = bookDto.Publisher;
                book.PublishedDate = bookDto.PublishedDate;
                book.CategoryId = bookDto.CategoryId;

                // Only update total copies if it's not less than borrowed copies
                var borrowedCopies = book.TotalCopies - book.AvailableCopies;
                if (bookDto.TotalCopies >= borrowedCopies)
                {
                    book.AvailableCopies = bookDto.TotalCopies - borrowedCopies;
                    book.TotalCopies = bookDto.TotalCopies;
                }
                else
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = $"Không thể giảm số lượng sách xuống dưới {borrowedCopies} (số sách đang được mượn)",
                        StatusCode = StatusCodes.Status400BadRequest
                    });
                }

                // Update author relationships
                var existingBookAuthors = await _context.BookAuthor
                    .Where(ba => ba.BookId == id)
                    .ToListAsync();
                _context.BookAuthor.RemoveRange(existingBookAuthors);

                for (int i = 0; i < bookDto.AuthorIds.Count; i++)
                {
                    var bookAuthor = new BookAuthor
                    {
                        BookId = id,
                        AuthorId = bookDto.AuthorIds[i],
                        IsPrimaryAuthor = i == 0
                    };
                    _context.BookAuthor.Add(bookAuthor);
                }

                await _context.SaveChangesAsync();

                // Load updated book data
                var updatedBook = await _context.Book
                    .Include(b => b.Category)
                    .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                    .FirstOrDefaultAsync(b => b.BookId == id);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = updatedBook,
                    Message = "Cập nhật sách thành công",
                    StatusCode = StatusCodes.Status200OK
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"Lỗi khi cập nhật sách: {ex.Message}",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }
        }

        // DELETE: api/Book/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteBook(int id)
        {
            var book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Sách không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            // Check if book is currently borrowed
            var isCurrentlyBorrowed = await _context.BorrowRecord
                .AnyAsync(br => br.BookId == id && !br.IsReturned);

            if (isCurrentlyBorrowed)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Không thể xóa sách đang được mượn",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            try
            {
                // Remove book-author relationships
                var bookAuthors = await _context.BookAuthor
                    .Where(ba => ba.BookId == id)
                    .ToListAsync();
                _context.BookAuthor.RemoveRange(bookAuthors);

                // Remove book
                _context.Book.Remove(book);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Sách đã được xóa",
                    StatusCode = StatusCodes.Status200OK
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"Lỗi khi xóa sách: {ex.Message}",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }
        }

        private bool BookExists(int id)
        {
            return _context.Book.Any(e => e.BookId == id);
        }
    }
}

