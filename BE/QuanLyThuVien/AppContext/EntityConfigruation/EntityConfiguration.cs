using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models.Entities;

namespace QuanLyThuVien.AppContext.EntityConfigruation
{
    public static class EntityConfiguration 
    {
        public static void AddEntityConfigraution(this ModelBuilder modelBuilder)
        {
            // Author configuration
            modelBuilder.Entity<Author>(entity =>
            {
                entity.HasIndex(e => e.FullName);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            });

            // BookAuthor configuration (Many-to-Many)
            modelBuilder.Entity<BookAuthor>(entity =>
            {
                entity.HasOne(d => d.Book)
                    .WithMany(p => p.BookAuthors)
                    .HasForeignKey(d => d.BookId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Author)
                    .WithMany(p => p.BookAuthors)
                    .HasForeignKey(d => d.AuthorId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint: one author can't be added twice to the same book
                entity.HasIndex(e => new { e.BookId, e.AuthorId }).IsUnique();
            });
            // Category configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(e => e.CategoryName).IsUnique();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            });

            // Book configuration
            modelBuilder.Entity<Book>(entity =>
            {
                entity.HasIndex(e => e.ISBN).IsUnique();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.Books)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Member configuration
            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasIndex(e => e.MemberCode).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.MembershipDate).HasDefaultValueSql("GETDATE()");
            });

            // BorrowRecord configuration
            modelBuilder.Entity<BorrowRecord>(entity =>
            {
                entity.HasOne(d => d.Member)
                    .WithMany(p => p.BorrowRecords)
                    .HasForeignKey(d => d.MemberId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Book)
                    .WithMany(p => p.BorrowRecords)
                    .HasForeignKey(d => d.BookId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.BorrowDate).HasDefaultValueSql("GETDATE()");
            });
        }
    }
}
