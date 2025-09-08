using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Events;

namespace UserService.Consumers
{
    /// <summary>
    /// Consumer for UserRegisteredEvent
    /// This consumer handles the UserRegisteredEvent by sending a welcome email to the new user
    /// </summary>
    public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
    {
        private readonly ILogger<UserRegisteredConsumer> _logger;

        public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
        {
            var userEvent = context.Message;

            try
            {
                _logger.LogInformation("Processing UserRegisteredEvent for user {UserId}, Email: {Email}", 
                    userEvent.UserId, userEvent.Email);

                // In a real implementation, this would send a welcome email
                // For now, we'll just log the event
                _logger.LogInformation("Welcome email would be sent to {Email} for user {UserId}", 
                    userEvent.Email, userEvent.UserId);

                // Additional processing logic could go here
                // For example, updating analytics, sending notifications, etc.

                _logger.LogInformation("Completed processing UserRegisteredEvent for user {UserId}", 
                    userEvent.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing UserRegisteredEvent for user {UserId}", 
                    userEvent.UserId);
                
                // Depending on requirements, we might want to:
                // 1. Retry the operation
                // 2. Send to a dead letter queue
                // 3. Notify administrators
                // For now, we'll rethrow to let MassTransit handle retries
                throw;
            }
        }
    }
}