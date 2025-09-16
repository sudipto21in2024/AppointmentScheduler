using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Models;
using ServiceManagementService.Services;

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
        Task<ValidationResult> ValidateCreateServiceRequestAsync(ServiceManagementService.Services.CreateServiceRequest request, Guid tenantId);

        /// <summary>
        /// Validates a service update request
        /// </summary>
        /// <param name="request">Service update request</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateUpdateServiceRequestAsync(ServiceManagementService.Services.UpdateServiceRequest request);

        /// <summary>
        /// Validates a category creation request
        /// </summary>
        /// <param name="request">Category creation request</param>
        /// <param name="tenantId">ID of the tenant</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateCreateCategoryRequestAsync(ServiceManagementService.Services.CreateCategoryRequest request, Guid tenantId);

        /// <summary>
        /// Validates a category update request
        /// </summary>
        /// <param name="request">Category update request</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateUpdateCategoryRequestAsync(ServiceManagementService.Services.UpdateCategoryRequest request);
    }

    /// <summary>
    /// Validation result model
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}