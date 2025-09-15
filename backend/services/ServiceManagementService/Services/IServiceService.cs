using Shared.Models;
using Shared.Events;

namespace ServiceManagementService.Services
{
    public interface IServiceService
    {
        /// <summary>
        /// Creates a new service
        /// </summary>
        /// <param name="request">Service creation request</param>
        /// <param name="userId">ID of the user creating the service</param>
        /// <param name="tenantId">ID of the tenant</param>
        /// <returns>Created service</returns>
        Task<Service> CreateServiceAsync(CreateServiceRequest request, Guid userId, Guid tenantId);

        /// <summary>
        /// Gets a service by ID
        /// </summary>
        /// <param name="serviceId">Service ID</param>
        /// <param name="userId">ID of the user requesting the service</param>
        /// <param name="tenantId">ID of the tenant</param>
        /// <returns>Service details</returns>
        Task<Service> GetServiceByIdAsync(Guid serviceId, Guid userId, Guid tenantId);

        /// <summary>
        /// Updates an existing service
        /// </summary>
        /// <param name="serviceId">Service ID</param>
        /// <param name="request">Service update request</param>
        /// <param name="userId">ID of the user updating the service</param>
        /// <param name="tenantId">ID of the tenant</param>
        /// <returns>Updated service</returns>
        Task<Service> UpdateServiceAsync(Guid serviceId, UpdateServiceRequest request, Guid userId, Guid tenantId);

        /// <summary>
        /// Deletes a service (soft delete)
        /// </summary>
        /// <param name="serviceId">Service ID</param>
        /// <param name="userId">ID of the user deleting the service</param>
        /// <param name="tenantId">ID of the tenant</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteServiceAsync(Guid serviceId, Guid userId, Guid tenantId);

        /// <summary>
        /// Gets all services with optional filtering
        /// </summary>
        /// <param name="userId">ID of the user requesting services</param>
        /// <param name="tenantId">ID of the tenant</param>
        /// <param name="categoryId">Optional category filter</param>
        /// <param name="isActive">Optional active status filter</param>
        /// <returns>List of services</returns>
        Task<IEnumerable<Service>> GetServicesAsync(Guid userId, Guid tenantId, Guid? categoryId = null, bool? isActive = null);

        /// <summary>
        /// Gets all service categories
        /// </summary>
        /// <param name="tenantId">ID of the tenant</param>
        /// <returns>List of service categories</returns>
        Task<IEnumerable<ServiceCategory>> GetServiceCategoriesAsync(Guid tenantId);

        /// <summary>
        /// Creates a new service category
        /// </summary>
        /// <param name="request">Category creation request</param>
        /// <param name="userId">ID of the user creating the category</param>
        /// <param name="tenantId">ID of the tenant</param>
        /// <returns>Created category</returns>
        Task<ServiceCategory> CreateServiceCategoryAsync(CreateCategoryRequest request, Guid userId, Guid tenantId);

        /// <summary>
        /// Updates an existing service category
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <param name="request">Category update request</param>
        /// <param name="userId">ID of the user updating the category</param>
        /// <param name="tenantId">ID of the tenant</param>
        /// <returns>Updated category</returns>
        Task<ServiceCategory> UpdateServiceCategoryAsync(Guid categoryId, UpdateCategoryRequest request, Guid userId, Guid tenantId);

        /// <summary>
        /// Deletes a service category
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <param name="userId">ID of the user deleting the category</param>
        /// <param name="tenantId">ID of the tenant</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteServiceCategoryAsync(Guid categoryId, Guid userId, Guid tenantId);
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