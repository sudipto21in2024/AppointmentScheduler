namespace Shared.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public Guid TenantId { get; set; }
        public UserRole Role { get; set; }
    }
}