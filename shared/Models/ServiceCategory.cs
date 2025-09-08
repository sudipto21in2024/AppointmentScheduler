using System;
using System.Collections.Generic;

namespace Shared.Models
{
    public class ServiceCategory
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public string? IconUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid TenantId { get; set; }

        // Navigation properties
        public virtual ServiceCategory? ParentCategory { get; set; }
        public virtual ICollection<ServiceCategory> SubCategories { get; set; } = new List<ServiceCategory>();
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
    }
}