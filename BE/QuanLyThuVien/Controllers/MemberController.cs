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
    public class MemberController : ControllerBase
    {
        private readonly AppDBContext _context;

        public MemberController(AppDBContext context)
        {
            _context = context;
        }

        // GET: api/Member
        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetMembers()
        {
            var members = await _context.Member
                .Where(m => m.IsActive)
                .OrderBy(m => m.FullName)
                .ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = members,
                Message = "Lấy danh sách thành viên thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Member/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse>> GetMember(int id)
        {
            var member = await _context.Member.FindAsync(id);

            if (member == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Thành viên không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            return Ok(new ApiResponse
            {
                Success = true,
                Data = member,
                Message = "Lấy thông tin thành viên thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Member/5/history
        [HttpGet("{id}/history")]
        public async Task<ActionResult<ApiResponse>> GetMemberHistory(int id)
        {
            var history = await _context.BorrowRecord
                .Include(br => br.Book)
                .ThenInclude(b => b.Category)
                .Include(br => br.Book)
                .ThenInclude(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .Where(br => br.MemberId == id)
                .OrderByDescending(br => br.BorrowDate)
                .ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = history,
                Message = "Lấy lịch sử mượn sách thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // GET: api/Member/5/current-borrows
        [HttpGet("{id}/current-borrows")]
        public async Task<ActionResult<ApiResponse>> GetMemberCurrentBorrows(int id)
        {
            var currentBorrows = await _context.BorrowRecord
                .Include(br => br.Book)
                    .ThenInclude(b => b.Category)
                .Include(br => br.Book)
                    .ThenInclude(b => b.BookAuthors)
                        .ThenInclude(ba => ba.Author)
                .Where(br => br.MemberId == id && !br.IsReturned)
                .OrderBy(br => br.DueDate)
                .ToListAsync();

            var borrowDtos = currentBorrows.Select(br => new BorrowRecordDtoMb
            {
                BorrowId = br.BorrowId,
                BorrowDate = br.BorrowDate,
                DueDate = br.DueDate,
                ReturnDate = br.ReturnDate,
                IsReturned = br.IsReturned,
                Notes = br.Notes,
                Book = new BookShortDtoMb
                {
                    BookId = br.Book.BookId,
                    Isbn = br.Book.ISBN,
                    Title = br.Book.Title,
                    Publisher = br.Book.Publisher,
                    PublishedDate = br.Book.PublishedDate,
                    CategoryId = br.Book.CategoryId,
                    CategoryName = br.Book.Category?.CategoryName,
                    Authors = br.Book.BookAuthors.Select(ba => new AuthorDtoMb
                    {
                        AuthorId = ba.Author.AuthorId,
                        Name = ba.Author.FullName
                    }).ToList()
                }
            }).ToList();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = borrowDtos,
                Message = "Lấy danh sách sách đang mượn thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }


        // GET: api/Member/search?term=nguyen
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse>> SearchMembers(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return await GetMembers();
            }

            var members = await _context.Member
                .Where(m => m.IsActive &&
                    (m.FullName.Contains(term) ||
                     m.MemberCode.Contains(term) ||
                     m.Email.Contains(term) ||
                     m.Phone.Contains(term)))
                .OrderBy(m => m.FullName)
                .ToListAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = members,
                Message = "Tìm kiếm thành viên thành công",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // POST: api/Member
        [HttpPost]
        public async Task<ActionResult<ApiResponse>> PostMember(Member member)
        {
            try
            {
                _context.Member.Add(member);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetMember", new { id = member.MemberId }, new ApiResponse
                {
                    Success = true,
                    Data = member,
                    Message = "Thêm thành viên thành công",
                    StatusCode = StatusCodes.Status201Created
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"Lỗi khi thêm thành viên: {ex.Message}",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }
        }

        // PUT: api/Member/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> PutMember(int id, Member member)
        {
            if (id != member.MemberId)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "ID không khớp",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            _context.Entry(member).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = member,
                    Message = "Cập nhật thành viên thành công",
                    StatusCode = StatusCodes.Status200OK
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MemberExists(id))
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Thành viên không tồn tại",
                        StatusCode = StatusCodes.Status404NotFound
                    });
                }
                throw;
            }
        }

        // PUT: api/Member/5/deactivate
        [HttpPut("{id}/deactivate")]
        public async Task<ActionResult<ApiResponse>> DeactivateMember(int id)
        {
            var member = await _context.Member.FindAsync(id);
            if (member == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Thành viên không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            // Check if member has unreturned books
            var hasUnreturnedBooks = await _context.BorrowRecord
                .AnyAsync(br => br.MemberId == id && !br.IsReturned);

            if (hasUnreturnedBooks)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Không thể hủy kích hoạt thành viên có sách chưa trả",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            member.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Data = member,
                Message = "Thành viên đã được hủy kích hoạt",
                StatusCode = StatusCodes.Status200OK
            });
        }

        // DELETE: api/Member/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteMember(int id)
        {
            var member = await _context.Member.FindAsync(id);
            if (member == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Thành viên không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }

            // Check if member has borrow history
            var hasBorrowHistory = await _context.BorrowRecord.AnyAsync(br => br.MemberId == id);
            if (hasBorrowHistory)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Không thể xóa thành viên có lịch sử mượn sách. Hãy sử dụng chức năng hủy kích hoạt.",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            _context.Member.Remove(member);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Thành viên đã được xóa",
                StatusCode = StatusCodes.Status200OK
            });
        }

        private bool MemberExists(int id)
        {
            return _context.Member.Any(e => e.MemberId == id);
        }
    }
    public class BorrowRecordDtoMb
    {
        public int BorrowId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; }
        public string Notes { get; set; }
        public BookShortDtoMb Book { get; set; }
    }

    public class BookShortDtoMb
    {
        public int BookId { get; set; }
        public string Isbn { get; set; }
        public string Title { get; set; }
        public string Publisher { get; set; }
        public DateTime PublishedDate { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<AuthorDtoMb> Authors { get; set; }
    }

    public class AuthorDtoMb
    {
        public int AuthorId { get; set; }
        public string Name { get; set; }
    }

}
