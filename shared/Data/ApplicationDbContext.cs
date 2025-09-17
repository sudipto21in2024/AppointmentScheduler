using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Microsoft.AspNetCore.Http; // Required for IHttpContextAccessor
using System.Security.Claims; // Required for ClaimsPrincipal

namespace Shared.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // User Service Entities
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // Service Management Service Entities
        public DbSet<ServiceCategory> ServiceCategories { get; set; }
        public DbSet<Service> Services { get; set; }

        // Booking Service Entities
        public DbSet<Slot> Slots { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        // Payment Service Entities
        public DbSet<Payment> Payments { get; set; }

        // Review Service Entities
        public DbSet<Review> Reviews { get; set; }

        // Notification Service Entities
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }

        // Tenant Management Entities
        public DbSet<Tenant> Tenants { get; set; }

        // Booking History Entities
        public DbSet<BookingHistory> BookingHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User entity configuration
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

            // Tenant entity configuration
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);
                    
                entity.Property(e => e.Description)
                    .HasMaxLength(1000);
                    
                entity.Property(e => e.Domain)
                    .HasMaxLength(255);
                    
                entity.Property(e => e.LogoUrl)
                    .HasMaxLength(500);
                    
                entity.Property(e => e.ContactEmail)
                    .HasMaxLength(255);
                    
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                    
                entity.Property(e => e.UpdatedAt)
                    .IsRequired();
            });

            // Service Category entity configuration
            modelBuilder.Entity<ServiceCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);
                    
                entity.Property(e => e.Description)
                    .HasMaxLength(1000);
                    
                entity.Property(e => e.IconUrl)
                    .HasMaxLength(500);
                    
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                    
                entity.Property(e => e.UpdatedAt)
                    .IsRequired();
                    
                // Self-referencing relationship for category hierarchy
                entity.HasOne(e => e.ParentCategory)
                    .WithMany(e => e.SubCategories)
                    .HasForeignKey(e => e.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Service entity configuration
            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);
                    
                entity.Property(e => e.Description)
                    .IsRequired();
                    
                entity.Property(e => e.Price)
                    .HasColumnType("decimal(18,2)");
                    
                entity.Property(e => e.Currency)
                    .HasDefaultValue("USD")
                    .HasMaxLength(3);
                    
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                    
                entity.Property(e => e.UpdatedAt)
                    .IsRequired();
                    
                // Relationships
                entity.HasOne(e => e.Category)
                    .WithMany(e => e.Services)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Provider)
                    .WithMany(e => e.ProvidedServices)
                    .HasForeignKey(e => e.ProviderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Slot entity configuration
            modelBuilder.Entity<Slot>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                    
                entity.Property(e => e.UpdatedAt)
                    .IsRequired();
                    
                // Relationships
                entity.HasOne(e => e.Service)
                    .WithMany(e => e.Slots)
                    .HasForeignKey(e => e.ServiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Booking entity configuration
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .IsRequired();
                    
                entity.Property(e => e.BookingDate)
                    .IsRequired();
                    
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                    
                entity.Property(e => e.UpdatedAt)
                    .IsRequired();
                    
                entity.Property(e => e.Notes)
                    .HasMaxLength(1000);
                    
                // Relationships
                entity.HasOne(e => e.Customer)
                    .WithMany(e => e.Bookings)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Service)
                    .WithMany(e => e.Bookings)
                    .HasForeignKey(e => e.ServiceId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Slot)
                    .WithMany(e => e.Bookings)
                    .HasForeignKey(e => e.SlotId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Payment entity configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.PaymentMethod)
                    .HasConversion<string>()
                    .IsRequired();
                    
                entity.Property(e => e.PaymentStatus)
                    .HasConversion<string>()
                    .IsRequired();
                    
                entity.Property(e => e.Currency)
                    .HasDefaultValue("USD")
                    .HasMaxLength(3);
                    
                entity.Property(e => e.Amount)
                    .HasColumnType("decimal(18,2)");
                    
                entity.Property(e => e.RefundAmount)
                    .HasColumnType("decimal(18,2)");
                    
                entity.Property(e => e.TransactionId)
                    .HasMaxLength(255);
                    
                entity.Property(e => e.PaymentGateway)
                    .HasMaxLength(100);
                    
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                    
                entity.Property(e => e.UpdatedAt)
                    .IsRequired();
                    
                // Relationships
                entity.HasOne(e => e.Booking)
                    .WithMany(e => e.Payments)
                    .HasForeignKey(e => e.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Review entity configuration
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Title)
                    .HasMaxLength(255);
                    
                entity.Property(e => e.Comment)
                    .HasMaxLength(2000);
                    
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                    
                entity.Property(e => e.UpdatedAt)
                    .IsRequired();
                    
                // Relationships
                entity.HasOne(e => e.Service)
                    .WithMany(e => e.Reviews)
                    .HasForeignKey(e => e.ServiceId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Customer)
                    .WithMany(e => e.Reviews)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Notification entity configuration
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(255);
                    
                entity.Property(e => e.Message)
                    .IsRequired();
                    
                entity.Property(e => e.Type)
                    .HasConversion<string>()
                    .IsRequired();
                    
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                    
                entity.Property(e => e.SentAt)
                    .IsRequired();
                    
                // Relationships
                entity.HasOne(e => e.User)
                    .WithMany(e => e.Notifications)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Notification Preference entity configuration
            modelBuilder.Entity<NotificationPreference>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.EmailEnabled)
                    .IsRequired();

                entity.Property(e => e.SmsEnabled)
                    .IsRequired();

                entity.Property(e => e.PushEnabled)
                    .IsRequired();

                entity.Property(e => e.PreferredTimezone)
                    .HasMaxLength(100);

                // Relationships
                entity.HasOne(e => e.User)
                    .WithMany(e => e.NotificationPreferences)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Booking History entity configuration
            modelBuilder.Entity<BookingHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.OldStatus)
                    .HasConversion<string>();
                    
                entity.Property(e => e.NewStatus)
                    .HasConversion<string>()
                    .IsRequired();
                    
                entity.Property(e => e.ChangeReason)
                    .HasMaxLength(500);
                    
                entity.Property(e => e.Notes)
                    .HasMaxLength(100);
                    
                entity.Property(e => e.ChangedAt)
                    .IsRequired();
                    
                // Relationships
                entity.HasOne(e => e.Booking)
                    .WithMany(e => e.History)
                    .HasForeignKey(e => e.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.ChangedByUser)
                    .WithMany(e => e.BookingHistories)
                    .HasForeignKey(e => e.ChangedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Apply multi-tenancy query filters
            modelBuilder.Entity<User>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<ServiceCategory>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Service>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Slot>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Booking>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Payment>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Review>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Notification>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<NotificationPreference>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<BookingHistory>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => e.User.TenantId == GetCurrentTenantId());

            // Create indexes for better performance
            modelBuilder.Entity<User>()
                .HasIndex(e => new { e.TenantId, e.Email })
                .IsUnique();
                
            modelBuilder.Entity<Service>()
                .HasIndex(e => new { e.TenantId, e.Name })
                .IsUnique();
                
            modelBuilder.Entity<Service>()
                .HasIndex(e => e.ProviderId);
                
            modelBuilder.Entity<Slot>()
                .HasIndex(e => e.ServiceId);
                
            modelBuilder.Entity<Booking>()
                .HasIndex(e => e.CustomerId);
                
            modelBuilder.Entity<Booking>()
                .HasIndex(e => e.ServiceId);
                
            modelBuilder.Entity<Booking>()
                .HasIndex(e => e.SlotId);
                
            modelBuilder.Entity<Booking>()
                .HasIndex(e => e.Status);
                
            modelBuilder.Entity<Payment>()
                .HasIndex(e => e.BookingId);
                
            modelBuilder.Entity<Review>()
                .HasIndex(e => e.ServiceId);
                
            modelBuilder.Entity<Review>()
                .HasIndex(e => e.CustomerId);
                
            modelBuilder.Entity<Notification>()
                .HasIndex(e => e.UserId);
                
            modelBuilder.Entity<Notification>()
                .HasIndex(e => e.IsRead);
                
            modelBuilder.Entity<BookingHistory>()
                .HasIndex(e => e.BookingId);

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Expires).IsRequired();
                entity.Property(e => e.Created).IsRequired();
                entity.Property(e => e.CreatedByIp).HasMaxLength(45); // IPv6 max length
                entity.Property(e => e.Revoked);
                entity.Property(e => e.RevokedByIp).HasMaxLength(45);
                entity.Property(e => e.ReplacedByToken).HasMaxLength(255);

                entity.HasOne(e => e.User)
                    .WithMany() // No navigation property back to RefreshTokens in User model
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            base.OnModelCreating(modelBuilder);
        }

        // Property to allow overriding the tenant ID for testing
        public static Guid? OverrideTenantId { get; set; } = null;
        
        // Placeholder for tenant ID - this would be implemented based on your tenant resolution strategy
        private Guid GetCurrentTenantId()
        {
            // If there's an override tenant ID, use it (for testing purposes)
            if (OverrideTenantId.HasValue)
            {
                return OverrideTenantId.Value;
            }

            // Retrieve TenantId from HttpContext.User.Claims
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && httpContext.User.Identity is ClaimsIdentity claimsIdentity)
            {
                var tenantIdClaim = claimsIdentity.FindFirst("TenantId");
                if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out Guid tenantId))
                {
                    return tenantId;
                }
            }

            // Fallback: If no tenant ID is found in claims, return Guid.Empty or throw an exception
            // depending on your application's security policy. Returning Guid.Empty might allow
            // access to non-tenant-specific data or indicate an unauthenticated context.
            return Guid.Empty;
        }
    }
}