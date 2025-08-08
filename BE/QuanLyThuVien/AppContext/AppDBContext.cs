using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.AppContext.EntityConfigruation;
using QuanLyThuVien.Models.Entities;
using System.Reflection;

namespace QuanLyThuVien.AppContext
{
    public class AppDBContext : IdentityDbContext<User, Role,Guid>
    {
        public AppDBContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Book> Book { get; set; }
        public DbSet<Author> Author { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<BorrowRecord> BorrowRecord { get; set; }
        public DbSet<Member> Member { get; set; }
        public DbSet<BookAuthor> BookAuthor { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            base.OnConfiguring(builder);
           
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.AddEntityConfigraution();

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (tableName.StartsWith("AspNet"))
                {
                    entityType.SetTableName(tableName.Substring(6));
                }
            }
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        }

    }
    public static class AppDBContextSeeder
    {
        public static void SeedData(AppDBContext context)
        {
            var rand = new Random();

            // Danh sách tên danh mục, tác giả, nhà xuất bản, địa chỉ, v.v. bằng tiếng Việt
            var categoryNames = new[]
            {
                "Khoa học", "Văn học", "Lịch sử", "Thiếu nhi", "Kinh tế", "Tâm lý", "Công nghệ", "Y học", "Du lịch", "Nghệ thuật"
            };
            var publisherNames = new[]
            {
                "NXB Trẻ", "NXB Kim Đồng", "NXB Giáo Dục", "NXB Văn Học", "NXB Lao Động"
            };
            var authorFirstNames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Vũ", "Đặng", "Bùi", "Đỗ", "Hồ" };
            var authorLastNames = new[] { "Anh", "Bình", "Châu", "Dũng", "Giang", "Hà", "Khánh", "Lan", "Minh", "Phương", "Quang", "Sơn", "Thảo", "Trang", "Tuấn", "Việt" };
            var memberFirstNames = new[] { "Ngọc", "Hải", "Hương", "Quỳnh", "Tú", "Thịnh", "Thủy", "Hùng", "Mai", "Tâm" };
            var memberLastNames = new[] { "Linh", "Nam", "Phúc", "Thắng", "Yến", "Hạnh", "Khoa", "Long", "Nhung", "Phát" };
            var addresses = new[] { "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng", "Cần Thơ", "Huế", "Nha Trang", "Vũng Tàu", "Biên Hòa", "Bắc Ninh", "Quảng Ninh" };

            // Seed Categories
            if (!context.Category.Any())
            {
                var categories = categoryNames.Select((name, i) => new Category
                {
                    CategoryName = name,
                    Description = $"Danh mục {name.ToLower()}",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }).ToList();

                context.Category.AddRange(categories);
                context.SaveChanges();
            }

            // Seed Authors
            if (!context.Author.Any())
            {
                var authors = Enumerable.Range(1, 20)
                    .Select(i =>
                    {
                        var firstName = authorFirstNames[rand.Next(authorFirstNames.Length)];
                        var lastName = authorLastNames[rand.Next(authorLastNames.Length)];
                        return new Author
                        {
                            FullName = $"{firstName} {lastName}",
                            PenName = $"Bút danh {lastName}",
                            Nationality = "Việt Nam",
                            Biography = $"Tác giả {firstName} {lastName} chuyên viết về {categoryNames[rand.Next(categoryNames.Length)]}.",
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        };
                    }).ToList();

                context.Author.AddRange(authors);
                context.SaveChanges();
            }

            // Seed Books
            if (!context.Book.Any())
            {
                var categoryIds = context.Category.Select(c => c.CategoryId).ToList();
                var books = new List<Book>();
                for (int i = 1; i <= 100; i++)
                {
                    var title = $"Cuốn sách số {i} về {categoryNames[rand.Next(categoryNames.Length)]}";
                    var authorName = $"{authorFirstNames[rand.Next(authorFirstNames.Length)]} {authorLastNames[rand.Next(authorLastNames.Length)]}";
                    books.Add(new Book
                    {
                        ISBN = $"978-604-{rand.Next(100000, 999999)}-{i % 10}",
                        Title = title,
                        Author = authorName,
                        Publisher = publisherNames[rand.Next(publisherNames.Length)],
                        PublishedDate = DateTime.Now.AddYears(-rand.Next(1, 10)),
                        CategoryId = categoryIds[rand.Next(categoryIds.Count)],
                        TotalCopies = rand.Next(2, 10),
                        AvailableCopies = rand.Next(1, 10),
                        CreatedDate = DateTime.Now
                    });
                }
                context.Book.AddRange(books);
                context.SaveChanges();

                // Seed BookAuthors (random 1-3 authors per book)
                var authorIds = context.Author.Select(a => a.AuthorId).ToList();
                var bookAuthors = new List<BookAuthor>();
                foreach (var book in books)
                {
                    var numAuthors = rand.Next(1, 4);
                    var selectedAuthors = authorIds.OrderBy(_ => rand.Next()).Take(numAuthors).ToList();
                    for (int j = 0; j < selectedAuthors.Count; j++)
                    {
                        bookAuthors.Add(new BookAuthor
                        {
                            BookId = book.BookId,
                            AuthorId = selectedAuthors[j],
                            IsPrimaryAuthor = j == 0
                        });
                    }
                }
                context.BookAuthor.AddRange(bookAuthors);
                context.SaveChanges();
            }

            // Seed Members
            if (!context.Member.Any())
            {
                var members = Enumerable.Range(1, 50)
                    .Select(i =>
                    {
                        var firstName = memberFirstNames[rand.Next(memberFirstNames.Length)];
                        var lastName = memberLastNames[rand.Next(memberLastNames.Length)];
                        return new Member
                        {
                            MemberCode = $"TV{i:D3}",
                            FullName = $"{firstName} {lastName}",
                            Email = $"tv{i}@thuviendemo.vn",
                            Phone = $"09{rand.Next(10000000, 99999999)}",
                            Address = addresses[rand.Next(addresses.Length)],
                            DateOfBirth = DateTime.Now.AddYears(-rand.Next(18, 60)),
                            MembershipDate = DateTime.Now.AddMonths(-rand.Next(1, 24)),
                            IsActive = true
                        };
                    }).ToList();

                context.Member.AddRange(members);
                context.SaveChanges();
            }

            // Seed BorrowRecords
            if (!context.BorrowRecord.Any())
            {
                var memberIds = context.Member.Select(m => m.MemberId).ToList();
                var bookIds = context.Book.Select(b => b.BookId).ToList();
                var borrowRecords = new List<BorrowRecord>();
                for (int i = 1; i <= 30; i++)
                {
                    var borrowDate = DateTime.Now.AddDays(-rand.Next(1, 60));
                    var dueDate = borrowDate.AddDays(14);
                    var isReturned = rand.NextDouble() > 0.3;
                    borrowRecords.Add(new BorrowRecord
                    {
                        MemberId = memberIds[rand.Next(memberIds.Count)],
                        BookId = bookIds[rand.Next(bookIds.Count)],
                        BorrowDate = borrowDate,
                        DueDate = dueDate,
                        ReturnDate = isReturned ? dueDate.AddDays(rand.Next(0, 7)) : null,
                        IsReturned = isReturned,
                        FineAmount = isReturned ? 0 : rand.Next(0, 100),
                        FinePaid = isReturned,
                        Notes = $"Lượt mượn số {i}"
                    });
                }
                context.BorrowRecord.AddRange(borrowRecords);
                context.SaveChanges();
            }
        }
    }
}
