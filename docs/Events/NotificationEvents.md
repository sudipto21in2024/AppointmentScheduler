# Notification Events

## Overview

This document describes the events related to notification management in the Multi-Tenant Appointment Booking System. These events are triggered when notifications are sent or fail to be delivered.

## Events

### NotificationSentEvent

**Description**: Triggered when a notification is successfully sent to a user.

**Triggered By**: NotificationService

**Consumed By**: 
- ReportingService (to update notification analytics)
- AuditService (for compliance tracking)

**Event Structure**:
```csharp
public class NotificationSentEvent : IEvent
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string NotificationType { get; set; } // Email, SMS, Push
    public string Channel { get; set; } // Welcome, BookingConfirmation, Reminder, etc.
    public string Title { get; set; }
    public string Message { get; set; }
    public DateTime SentAt { get; set; }
    public string Provider { get; set; } // SendGrid, Twilio, etc.
}
```

**Business Rules**:
1. This event is published after successful notification delivery
2. Contains all necessary notification information for analytics
3. Includes tenant information for multi-tenancy support
4. Different notification types have different priority levels

### NotificationFailedEvent

**Description**: Triggered when a notification fails to be delivered to a user.

**Triggered By**: NotificationService

**Consumed By**: 
- ReportingService (to track delivery failures)
- AlertService (to notify system administrators)
- RetryService (to attempt redelivery)

**Event Structure**:
```csharp
public class NotificationFailedEvent : IEvent
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string NotificationType { get; set; } // Email, SMS, Push
    public string Channel { get; set; } // Welcome, BookingConfirmation, Reminder, etc.
    public string Title { get; set; }
    public string Message { get; set; }
    public string FailureReason { get; set; }
    public DateTime FailedAt { get; set; }
    public string Provider { get; set; } // SendGrid, Twilio, etc.
}
```

## Implementation Guidelines for AI Coding Agents

1. **Event Publishing**: All notification events should be published using MassTransit's `IPublishEndpoint`
2. **Validation**: Validate notification data before publishing events
3. **Consistency**: Ensure events contain consistent data with the notification entity
4. **Timing**: Publish events after delivery attempts are completed
5. **Error Handling**: Implement proper error handling and logging for event publishing
6. **Idempotency**: Ensure event handlers are idempotent to handle duplicate messages
7. **Retry Logic**: Implement retry logic for failed notifications
8. **Security**: Ensure sensitive information is not included in events

## Example Implementation

### Publishing an Event
```csharp
public class NotificationService : INotificationService
{
    private readonly IPublishEndpoint _publishEndpoint;
    
    public async Task SendNotificationAsync(SendNotificationRequest request)
    {
        try
        {
            // Send notification logic here
            
            // Publish success event
            var notificationSentEvent = new NotificationSentEvent
            {
                NotificationId = notification.Id,
                UserId = notification.UserId,
                TenantId = notification.TenantId,
                NotificationType = notification.Type,
                Channel = notification.Channel,
                Title = notification.Title,
                Message = notification.Message,
                SentAt = DateTime.UtcNow,
                Provider = notification.Provider
            };
            
            await _publishEndpoint.Publish(notificationSentEvent);
        }
        catch (Exception ex)
        {
            // Publish failure event
            var notificationFailedEvent = new NotificationFailedEvent
            {
                NotificationId = notification.Id,
                UserId = notification.UserId,
                TenantId = notification.TenantId,
                NotificationType = notification.Type,
                Channel = notification.Channel,
                Title = notification.Title,
                Message = notification.Message,
                FailureReason = ex.Message,
                FailedAt = DateTime.UtcNow,
                Provider = notification.Provider
            };
            
            await _publishEndpoint.Publish(notificationFailedEvent);
        }
    }
}
```

### Consuming an Event
```csharp
public class NotificationSentConsumer : IConsumer<NotificationSentEvent>
{
    private readonly ILogger<NotificationSentConsumer> _logger;
    private readonly IReportingService _reportingService;
    
    public async Task Consume(ConsumeContext<NotificationSentEvent> context)
    {
        var notificationEvent = context.Message;
        
        // Update notification analytics
        await _reportingService.UpdateNotificationMetricsAsync(new NotificationMetrics
        {
            NotificationType = notificationEvent.NotificationType,
            Channel = notificationEvent.Channel,
            TenantId = notificationEvent.TenantId,
            SentAt = notificationEvent.SentAt
        });
        
        _logger.LogInformation("Notification sent: {NotificationId}, Type: {Type}", 
            notificationEvent.NotificationId, notificationEvent.NotificationType);
    }
}