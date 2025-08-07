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
}
