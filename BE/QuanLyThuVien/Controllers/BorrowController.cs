using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.AppContext;
using QuanLyThuVien.Data;
using QuanLyThuVien.Models.DTOs;
using QuanLyThuVien.Models.DTOs.BorrowRecordModels;
using QuanLyThuVien.Models.Entities;

namespace QuanLyThuVien.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = RoleName.Admin)]
    public class BorrowController : ControllerBase
    {
        private readonly AppDBContext _context;

        public BorrowController(AppDBContext context)
        {
            _context = context;
        }

        // GET: api/Borrow/current
        [HttpGet("current")]
        public async Task<ActionResult<ApiResponse>> GetCurrentBorrows()
        {
            var borrowRecords = await _context.BorrowRecord
                .Include(br => br.Book)
                .ThenInclude(b => b.Category)
                .Include(br => br.Book)
                .ThenInclude(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .Include(br => br.Member)
                .Where(br => !br.IsReturned)
                .OrderBy(br => br.DueDate)
                .ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = borrowRecords,
                Message = "Lấy danh sách mượn hiện tại thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Borrow/overdue
        [HttpGet("overdue")]
        public async Task<ActionResult<ApiResponse>> GetOverdueBooks()
        {
            var overdueBooks = await _context.BorrowRecord
                .Include(br => br.Book)
                .ThenInclude(b => b.Category)
                .Include(br => br.Book)
                .ThenInclude(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .Include(br => br.Member)
                .Where(br => !br.IsReturned && br.DueDate < DateTime.Now)
                .OrderBy(br => br.DueDate)
                .ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = overdueBooks,
                Message = "Lấy danh sách sách quá hạn thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Borrow/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse>> GetBorrowRecord(int id)
        {
            var borrowRecord = await _context.BorrowRecord
                .Include(br => br.Book)
                .ThenInclude(b => b.Category)
                .Include(br => br.Book)
                .ThenInclude(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .Include(br => br.Member)
                .FirstOrDefaultAsync(br => br.BorrowId == id);

            if (borrowRecord == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Bản ghi mượn không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            return Ok(new ApiResponse
            {
                Success = true,
                Data = borrowRecord,
                Message = "Lấy thông tin mượn thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // POST: api/Borrow
        [HttpPost]
        public async Task<ActionResult<ApiResponse>> BorrowBook(BorrowRequestDto request)
        {
            var book = await _context.Book.FindAsync(request.BookId);
            var member = await _context.Member.FindAsync(request.MemberId);

            if (book == null || member == null)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Book hoặc Member không tồn tại",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            if (book.AvailableCopies <= 0)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Sách đã hết",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            if (!member.IsActive)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Thành viên không hoạt động",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            // Check if member has overdue books
            var hasOverdueBooks = await _context.BorrowRecord
                .AnyAsync(br => br.MemberId == request.MemberId &&
                               !br.IsReturned &&
                               br.DueDate < DateTime.Now);

            if (hasOverdueBooks)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Thành viên có sách quá hạn chưa trả",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            // Check borrowing limit (max 5 books at once)
            var currentBorrowCount = await _context.BorrowRecord
                .CountAsync(br => br.MemberId == request.MemberId && !br.IsReturned);

            if (currentBorrowCount >= 5)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Thành viên đã mượn tối đa 5 quyển sách",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            try
            {
                var borrowRecord = new BorrowRecord
                {
                    MemberId = request.MemberId,
                    BookId = request.BookId,
                    BorrowDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(request.BorrowDays ?? 14),
                    Notes = request.Notes
                };

                book.AvailableCopies--;

                _context.BorrowRecord.Add(borrowRecord);
                await _context.SaveChangesAsync();

                // Load full borrow record
                var createdRecord = await _context.BorrowRecord
                    .Include(br => br.Book)
                    .ThenInclude(b => b.Category)
                    .Include(br => br.Book)
                    .ThenInclude(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                    .Include(br => br.Member)
                    .FirstOrDefaultAsync(br => br.BorrowId == borrowRecord.BorrowId);

                var response = new ApiResponse
                {
                    Success = true,
                    Data = createdRecord,
                    Message = "Mượn sách thành công",
                    StatusCode = StatusCodes.Status201Created
                };

                return CreatedAtAction("GetBorrowRecord", new { id = borrowRecord.BorrowId }, response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"Lỗi khi mượn sách: {ex.Message}",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }
        }

        // PUT: api/Borrow/5/return
        [HttpPut("{id}/return")]
        public async Task<ActionResult<ApiResponse>> ReturnBook(int id)
        {
            var borrowRecord = await _context.BorrowRecord
                .Include(br => br.Book)
                .FirstOrDefaultAsync(br => br.BorrowId == id);

            if (borrowRecord == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Bản ghi mượn không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            if (borrowRecord.IsReturned)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Sách đã được trả",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            try
            {
                borrowRecord.ReturnDate = DateTime.Now;
                borrowRecord.IsReturned = true;
                borrowRecord.Book.AvailableCopies++;

                // Calculate fine if overdue
                if (DateTime.Now > borrowRecord.DueDate)
                {
                    var overdueDays = (DateTime.Now - borrowRecord.DueDate).Days;
                    borrowRecord.FineAmount = overdueDays * 5000; // 5000 VND per day
                }

                await _context.SaveChangesAsync();

                var updatedRecord = await _context.BorrowRecord
                    .Include(br => br.Book)
                    .ThenInclude(b => b.Category)
                    .Include(br => br.Book)
                    .ThenInclude(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                    .Include(br => br.Member)
                    .FirstOrDefaultAsync(br => br.BorrowId == id);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = updatedRecord,
                    Message = "Trả sách thành công",
                    StatusCode = StatusCodes.Status200OK
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"Lỗi khi trả sách: {ex.Message}",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }
        }

        // PUT: api/Borrow/5/extend
        [HttpPut("{id}/extend")]
        public async Task<ActionResult<ApiResponse>> ExtendBorrow(int id, ExtendBorrowDto request)
        {
            var borrowRecord = await _context.BorrowRecord.FindAsync(id);

            if (borrowRecord == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Bản ghi mượn không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            if (borrowRecord.IsReturned)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Sách đã được trả",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            if (DateTime.Now > borrowRecord.DueDate)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Không thể gia hạn sách quá hạn",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            try
            {
                borrowRecord.DueDate = borrowRecord.DueDate.AddDays(request.ExtendDays);
                borrowRecord.Notes = request.Notes ?? borrowRecord.Notes;

                await _context.SaveChangesAsync();

                var updatedRecord = await _context.BorrowRecord
                    .Include(br => br.Book)
                    .ThenInclude(b => b.Category)
                    .Include(br => br.Book)
                    .ThenInclude(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                    .Include(br => br.Member)
                    .FirstOrDefaultAsync(br => br.BorrowId == id);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = updatedRecord,
                    Message = "Gia hạn mượn sách thành công",
                    StatusCode = StatusCodes.Status200OK
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"Lỗi khi gia hạn: {ex.Message}",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }
        }

        // PUT: api/Borrow/5/pay-fine
        [HttpPut("{id}/pay-fine")]
        public async Task<ActionResult<ApiResponse>> PayFine(int id)
        {
            var borrowRecord = await _context.BorrowRecord.FindAsync(id);

            if (borrowRecord == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Bản ghi mượn không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            if (borrowRecord.FineAmount <= 0)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Không có phí phạt cần thanh toán",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            borrowRecord.FinePaid = true;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = borrowRecord,
                Message = $"Đã thanh toán phí phạt {borrowRecord.FineAmount:N0} VND",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Borrow/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse>> GetBorrowStatistics()
        {
            var stats = new
            {
                TotalBorrows = await _context.BorrowRecord.CountAsync(),
                CurrentBorrows = await _context.BorrowRecord.CountAsync(br => !br.IsReturned),
                OverdueBooks = await _context.BorrowRecord.CountAsync(br => !br.IsReturned && br.DueDate < DateTime.Now),
                TotalFines = await _context.BorrowRecord.SumAsync(br => br.FineAmount),
                UnpaidFines = await _context.BorrowRecord
                    .Where(br => br.FineAmount > 0 && !br.FinePaid)
                    .SumAsync(br => br.FineAmount),
                PopularBooks = await _context.BorrowRecord
                    .Include(br => br.Book)
                    .GroupBy(br => br.Book)
                    .Select(g => new
                    {
                        Book = g.Key.Title,
                        BorrowCount = g.Count()
                    })
                    .OrderByDescending(x => x.BorrowCount)
                    .Take(5)
                    .ToListAsync()
            };

            return Ok(new ApiResponse
            {
                Success = true,
                Data = stats,
                Message = "Lấy thống kê mượn sách thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }
    }
}
