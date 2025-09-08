using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Events;

namespace BookingService.Consumers
{
    /// <summary>
    /// Consumer for BookingCreatedEvent
    /// This consumer handles the BookingCreatedEvent by initiating payment processing
    /// </summary>
    public class BookingCreatedConsumer : IConsumer<BookingCreatedEvent>
    {
        private readonly ILogger<BookingCreatedConsumer> _logger;

        public BookingCreatedConsumer(ILogger<BookingCreatedConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<BookingCreatedEvent> context)
        {
            var bookingEvent = context.Message;

            try
            {
                _logger.LogInformation("Processing BookingCreatedEvent for booking {BookingId}, Customer: {CustomerId}", 
                    bookingEvent.BookingId, bookingEvent.CustomerId);

                // In a real implementation, this would initiate payment processing
                // For now, we'll just log the event
                _logger.LogInformation("Payment processing would be initiated for booking {BookingId}", 
                    bookingEvent.BookingId);

                // Additional processing logic could go here
                // For example, updating availability, sending notifications, etc.

                _logger.LogInformation("Completed processing BookingCreatedEvent for booking {BookingId}", 
                    bookingEvent.BookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing BookingCreatedEvent for booking {BookingId}", 
                    bookingEvent.BookingId);
                
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