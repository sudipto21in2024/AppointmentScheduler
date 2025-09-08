using Microsoft.AspNetCore.Mvc;
using Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ServiceManagementService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ServiceController : ControllerBase
    {
        private readonly ILogger<ServiceController> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public ServiceController(ILogger<ServiceController> logger, IPublishEndpoint publishEndpoint)
        {
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
        {
            // In a real implementation, this would create a service in the database
            // For now, we'll just simulate the creation and publish the event
            
            var serviceId = Guid.NewGuid();
            
            _logger.LogInformation($"Service created with ID: {serviceId}");
            
            // Publish ServiceCreatedEvent
            var serviceCreatedEvent = new ServiceCreatedEvent
            {
                ServiceId = serviceId,
                Name = request.Name,
                Description = request.Description,
                CategoryId = request.CategoryId,
                ProviderId = request.ProviderId,
                TenantId = request.TenantId,
                Price = request.Price,
                Currency = request.Currency,
                Duration = request.Duration,
                CreatedAt = DateTime.UtcNow
            };
            
            await _publishEndpoint.Publish(serviceCreatedEvent);
            
            _logger.LogInformation($"ServiceCreatedEvent published for service {serviceId}");
            
            return Ok(new { ServiceId = serviceId, Message = "Service created successfully" });
        }
        
        [HttpPut("{id}/publish")]
        public async Task<IActionResult> PublishService(Guid id)
        {
            // In a real implementation, this would update the service status in the database
            // For now, we'll just simulate the publishing and publish the event
            
            _logger.LogInformation($"Service published with ID: {id}");
            
            // Publish ServicePublishedEvent
            var servicePublishedEvent = new ServicePublishedEvent
            {
                ServiceId = id,
                Name = "Sample Service",
                ProviderId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                PublishedAt = DateTime.UtcNow
            };
            
            await _publishEndpoint.Publish(servicePublishedEvent);
            
            _logger.LogInformation($"ServicePublishedEvent published for service {id}");
            
            return Ok(new { ServiceId = id, Message = "Service published successfully" });
        }
    }
    
    public class CreateServiceRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public Guid ProviderId { get; set; }
        public Guid TenantId { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public int Duration { get; set; }
    }
}