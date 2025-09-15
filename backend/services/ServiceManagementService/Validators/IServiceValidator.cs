using Shared.Models;

namespace ServiceManagementService.Validators
{
    public interface IServiceValidator
    {
        /// <summary>
        /// Validates a service creation request
        /// </summary>
        /// <param name="request">Service creation request</param>
        /// <param name="tenantId">ID of the tenant</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateCreateServiceRequestAsync(CreateServiceRequest request, Guid tenantId);

        /// <summary>
        /// Validates a service update request
        /// </summary>
        /// <param name="request">Service update request</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateUpdateServiceRequestAsync(UpdateServiceRequest request);

        /// <summary>
        /// Validates a category creation request
        /// </summary>
        /// <param name="request">Category creation request</param>
        /// <param name="tenantId">ID of the tenant</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateCreateCategoryRequestAsync(CreateCategoryRequest request, Guid tenantId);

        /// <summary>
        /// Validates a category update request
        /// </summary>
        /// <param name="request">Category update request</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateUpdateCategoryRequestAsync(UpdateCategoryRequest request);
    }

    /// <summary>
    /// Validation result model
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Request model for creating a service
    /// </summary>
    public class CreateServiceRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public int Duration { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public int MaxBookingsPerDay { get; set; } = 10;
    }

    /// <summary>
    /// Request model for updating a service
    /// </summary>
    public class UpdateServiceRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Guid? CategoryId { get; set; }
        public int? Duration { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsFeatured { get; set; }
        public int? MaxBookingsPerDay { get; set; }
    }

    /// <summary>
    /// Request model for creating a service category
    /// </summary>
    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public string? IconUrl { get; set; }
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Request model for updating a service category
    /// </summary>
    public class UpdateCategoryRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public string? IconUrl { get; set; }
        public int? SortOrder { get; set; }
        public bool? IsActive { get; set; }
    }
}