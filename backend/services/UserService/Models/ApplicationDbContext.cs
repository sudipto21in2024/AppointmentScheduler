using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace UserService.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasIndex(e => e.Email).IsUnique();
                
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);
                    
                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);
                    
                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);
                    
                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(100);
                    
                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(20);
                    
                entity.Property(e => e.UserType)
                    .HasConversion<string>()
                    .IsRequired();
                    
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                    
                entity.Property(e => e.UpdatedAt)
                    .IsRequired();
                    
                entity.Property(e => e.LastLoginAt);
            });
        }
    }
}