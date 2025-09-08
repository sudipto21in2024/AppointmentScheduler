using Microsoft.AspNetCore.Mvc;
using Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger<NotificationController> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public NotificationController(ILogger<NotificationController> logger, IPublishEndpoint publishEndpoint)
        {
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
        {
            // In a real implementation, this would send a notification through various channels
            // For now, we'll just simulate the sending and publish the appropriate event
            
            var notificationId = Guid.NewGuid();
            
            _logger.LogInformation($"Sending notification with ID: {notificationId}");
            
            // Simulate notification sending (in a real implementation, this would call email/SMS services)
            var isNotificationSent = true; // Simulate success
            
            if (isNotificationSent)
            {
                // Publish NotificationSentEvent
                var notificationSentEvent = new NotificationSentEvent
                {
                    NotificationId = notificationId,
                    UserId = request.UserId,
                    TenantId = request.TenantId,
                    NotificationType = request.NotificationType,
                    Channel = request.Channel,
                    Title = request.Title,
                    Message = request.Message,
                    SentAt = DateTime.UtcNow,
                    Provider = "SimulatedProvider"
                };
                
                await _publishEndpoint.Publish(notificationSentEvent);
                
                _logger.LogInformation($"NotificationSentEvent published for notification {notificationId}");
                
                return Ok(new { NotificationId = notificationId, Message = "Notification sent successfully" });
            }
            else
            {
                // Publish NotificationFailedEvent
                var notificationFailedEvent = new NotificationFailedEvent
                {
                    NotificationId = notificationId,
                    UserId = request.UserId,
                    TenantId = request.TenantId,
                    NotificationType = request.NotificationType,
                    Channel = request.Channel,
                    Title = request.Title,
                    Message = request.Message,
                    FailureReason = "Simulated notification failure",
                    FailedAt = DateTime.UtcNow,
                    Provider = "SimulatedProvider"
                };
                
                await _publishEndpoint.Publish(notificationFailedEvent);
                
                _logger.LogInformation($"NotificationFailedEvent published for notification {notificationId}");
                
                return BadRequest(new { NotificationId = notificationId, Message = "Notification sending failed" });
            }
        }
    }
    
    public class SendNotificationRequest
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string NotificationType { get; set; } = string.Empty; // Email, SMS, Push
        public string Channel { get; set; } = string.Empty; // Welcome, BookingConfirmation, Reminder, etc.
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}