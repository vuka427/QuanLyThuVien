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
                .OrderByDescending(br => br.DueDate)
                .ToListAsync();

            var borrowRecordDtos = borrowRecords.Select(br => new BorrowRecordDto
            {
                BorrowId = br.BorrowId,
                MemberId = br.MemberId,
                BookId = br.BookId,
                BorrowDate = br.BorrowDate,
                DueDate = br.DueDate,
                ReturnDate = br.ReturnDate,
                IsReturned = br.IsReturned,
                FineAmount = br.FineAmount,
                FinePaid = br.FinePaid,
                Notes = br.Notes,
                Member = new MemberShortDto
                {
                    MemberId = br.Member.MemberId,
                    MemberCode = br.Member.MemberCode,
                    FullName = br.Member.FullName
                },
                Book = new BookShortDto
                {
                    BookId = br.Book.BookId,
                    Title = br.Book.Title,
                    Isbn = br.Book.ISBN,
                    CategoryName = br.Book.Category?.CategoryName
                }
            }).ToList();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = borrowRecordDtos,
                Message = "Lấy danh sách mượn hiện tại thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }


        // GET: api/Borrow/overdue
        [HttpGet("overdue")]
        public async Task<ActionResult<ApiResponse>> GetOverdueBooks()
        {
           

            var now = DateTime.UtcNow;
            const decimal finePerDay = 5000m;

            // Chỉ query các cột cần thiết và PROJECT sang DTO để tránh vòng lặp
            var baseQuery = _context.BorrowRecord
                .AsNoTracking()
                .Where(br => !br.IsReturned && br.DueDate < now)
                .OrderBy(br => br.DueDate)
                .Select(br => new BorrowRecordDto
                {
                    BorrowId = br.BorrowId,
                    MemberId = br.MemberId,
                    BookId = br.BookId,
                    BorrowDate = br.BorrowDate,
                    DueDate = br.DueDate,
                    ReturnDate = br.ReturnDate,
                    IsReturned = br.IsReturned,
                    
                    FineAmount = (decimal)Math.Ceiling(
                                    EF.Functions.DateDiffSecond(br.DueDate, now) / 86400.0
                                 ) * finePerDay,
                    FinePaid = br.FinePaid,
                    Notes = br.Notes,

                    Member = new MemberShortDto
                    {
                        MemberId = br.Member.MemberId,
                        MemberCode = br.Member.MemberCode,
                        FullName = br.Member.FullName,
                        Email = br.Member.Email
                    },
                    Book = new BookShortDto
                    {
                        BookId = br.Book.BookId,
                        Title = br.Book.Title,
                        Isbn = br.Book.ISBN,
                        CategoryName = br.Book.Category != null ? br.Book.Category.CategoryName : null,
                        Publisher = br.Book.Publisher
                    }
                });

            
            var items = await baseQuery.ToListAsync();

            // Trả về DTO, không trả entity ⇒ không còn vòng tham chiếu
            return Ok(new ApiResponse
            {
                Success = true,
                Data = items,
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

                // Load full borrow record kèm các thông tin cần để map sang DTO
                var createdRecord = await _context.BorrowRecord
                    .Include(br => br.Book)
                        .ThenInclude(b => b.Category)
                    .Include(br => br.Member)
                    .FirstOrDefaultAsync(br => br.BorrowId == borrowRecord.BorrowId);

                // Map sang DTO, không vòng lặp navigation
                var createdRecordDto = new BorrowRecordDto
                {
                    BorrowId = createdRecord.BorrowId,
                    MemberId = createdRecord.MemberId,
                    BookId = createdRecord.BookId,
                    BorrowDate = createdRecord.BorrowDate,
                    DueDate = createdRecord.DueDate,
                    ReturnDate = createdRecord.ReturnDate,
                    IsReturned = createdRecord.IsReturned,
                    FineAmount = createdRecord.FineAmount,
                    FinePaid = createdRecord.FinePaid,
                    Notes = createdRecord.Notes,
                    Member = new MemberShortDto
                    {
                        MemberId = createdRecord.Member.MemberId,
                        MemberCode = createdRecord.Member.MemberCode,
                        FullName = createdRecord.Member.FullName,
                        Email = createdRecord.Member.Email
                    },
                    Book = new BookShortDto
                    {
                        BookId = createdRecord.Book.BookId,
                        Title = createdRecord.Book.Title,
                        Isbn = createdRecord.Book.ISBN,
                        Publisher = createdRecord.Book.Publisher,
                        CategoryName = createdRecord.Book.Category?.CategoryName
                    }
                };

                var response = new ApiResponse
                {
                    Success = true,
                    Data = createdRecordDto,
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


// PUT: api/Borrow/5/return
        [HttpPut("{id:int}/return")]

    public async Task<ActionResult<ApiResponse>> ReturnBook(int id)
    {
        // Khuyến nghị dùng UTC trong DB để nhất quán
        var now = DateTime.UtcNow;

        // Lấy bản ghi kèm Book/Member để cập nhật
        var borrowRecord = await _context.BorrowRecord
            .Include(br => br.Book)
                .ThenInclude(b => b.Category)
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

        if (borrowRecord.IsReturned)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "Sách đã được trả",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        if (borrowRecord.Book == null)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "Không tìm thấy sách tương ứng",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        if (borrowRecord.Member == null)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "Không tìm thấy thành viên tương ứng",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        using var trx = await _context.Database.BeginTransactionAsync();
        try
        {
            // Cập nhật trạng thái trả
            borrowRecord.ReturnDate = now;
            borrowRecord.IsReturned = true;

            // Tính phạt (làm tròn lên nếu trễ trong ngày)
            int overdueDays = 0;
            if (now > borrowRecord.DueDate)
            {
                overdueDays = (int)Math.Ceiling((now - borrowRecord.DueDate).TotalDays);
            }

            const decimal finePerDay = 5000m; // VND/ngày
            borrowRecord.FineAmount = overdueDays > 0 ? overdueDays * finePerDay : 0m;

            // Tăng tồn sách (chặn âm nhỡ logic khác đã làm giảm)
            borrowRecord.Book.AvailableCopies = Math.Max(0, borrowRecord.Book.AvailableCopies + 1);

            await _context.SaveChangesAsync();

            // Dựng DTO bằng projection (KHÔNG trả entity) → tránh vòng tham chiếu
            var dto = await _context.BorrowRecord
                .Where(br => br.BorrowId == id)
                .Select(br => new BorrowRecordDto
                {
                    BorrowId = br.BorrowId,
                    MemberId = br.MemberId,
                    BookId = br.BookId,
                    BorrowDate = br.BorrowDate,
                    DueDate = br.DueDate,
                    ReturnDate = br.ReturnDate,
                    IsReturned = br.IsReturned,
                    FineAmount = br.FineAmount,
                    FinePaid = br.FinePaid,
                    Notes = br.Notes,

                    Member = new MemberShortDto
                    {
                        MemberId = br.Member.MemberId,
                        MemberCode = br.Member.MemberCode,
                        FullName = br.Member.FullName,
                        Email = br.Member.Email
                    },
                    Book = new BookShortDto
                    {
                        BookId = br.Book.BookId,
                        Title = br.Book.Title,
                        Isbn = br.Book.ISBN,
                        CategoryName = br.Book.Category != null ? br.Book.Category.CategoryName : null,
                        Publisher = br.Book.Publisher
                    }
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            await trx.CommitAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = dto,
                Message = "Trả sách thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
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
        public async Task<ActionResult<ApiResponse>> ExtendBorrow(int id, [FromBody] ExtendBorrowDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            var now = DateTime.UtcNow;

            // Lấy bản ghi để kiểm tra & cập nhật
            var borrowRecord = await _context.BorrowRecord
                .Include(br => br.Book).ThenInclude(b => b.Category)
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

            if (borrowRecord.IsReturned)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Sách đã được trả",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            // Không cho gia hạn nếu đã quá hạn tại thời điểm gọi API
            if (now > borrowRecord.DueDate.ToUniversalTime())
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Không thể gia hạn sách quá hạn",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            // (Tuỳ chọn) Giới hạn số ngày gia hạn một lần
            if (request.ExtendDays <= 0 || request.ExtendDays > 60)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Số ngày gia hạn phải trong khoảng 1–60 ngày",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Cập nhật DueDate & ghi chú
                borrowRecord.DueDate = borrowRecord.DueDate.AddDays(request.ExtendDays);
                if (!string.IsNullOrWhiteSpace(request.Notes))
                {
                    borrowRecord.Notes = request.Notes;
                }

                await _context.SaveChangesAsync();

                // Project sang DTO để trả về (KHÔNG trả entity có Include)
                var dto = await _context.BorrowRecord
                    .Where(br => br.BorrowId == id)
                    .Select(br => new BorrowRecordDto
                    {
                        BorrowId = br.BorrowId,
                        MemberId = br.MemberId,
                        BookId = br.BookId,
                        BorrowDate = br.BorrowDate,
                        DueDate = br.DueDate,
                        ReturnDate = br.ReturnDate,
                        IsReturned = br.IsReturned,
                        FineAmount = br.FineAmount,
                        FinePaid = br.FinePaid,
                        Notes = br.Notes,
                        Member = new MemberShortDto
                        {
                            MemberId = br.Member.MemberId,
                            MemberCode = br.Member.MemberCode,
                            FullName = br.Member.FullName,
                            Email = br.Member.Email
                        },
                        Book = new BookShortDto
                        {
                            BookId = br.Book.BookId,
                            Title = br.Book.Title,
                            Isbn = br.Book.ISBN,
                            CategoryName = br.Book.Category != null ? br.Book.Category.CategoryName : null,
                            Publisher = br.Book.Publisher
                        }
                    })
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                await trx.CommitAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = dto,
                    Message = "Gia hạn mượn sách thành công",
                    StatusCode = StatusCodes.Status200OK
                });
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
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
    public class BorrowRecordDto
    {
        public int BorrowId { get; set; }
        public int MemberId { get; set; }
        public int BookId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; }
        public decimal FineAmount { get; set; }
        public bool FinePaid { get; set; }
        public string Notes { get; set; }
        public MemberShortDto Member { get; set; }
        public BookShortDto Book { get; set; }
    }

    public class MemberShortDto
    {
        public int MemberId { get; set; }
        public string MemberCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }

    public class BookShortDto
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Isbn { get; set; }
        public string CategoryName { get; set; }
        public string Publisher { get; set; }
    }

}
