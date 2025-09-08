using Microsoft.AspNetCore.Mvc;
using Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace PaymentService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public PaymentController(ILogger<PaymentController> logger, IPublishEndpoint publishEndpoint)
        {
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            // In a real implementation, this would process a payment through a payment gateway
            // For now, we'll just simulate the processing and publish the event
            
            var paymentId = Guid.NewGuid();
            
            _logger.LogInformation($"Processing payment with ID: {paymentId}");
            
            // Simulate payment processing (in a real implementation, this would call a payment gateway)
            var isPaymentSuccessful = true; // Simulate success
            
            if (isPaymentSuccessful)
            {
                // Publish PaymentProcessedEvent
                var paymentProcessedEvent = new PaymentProcessedEvent
                {
                    PaymentId = paymentId,
                    BookingId = request.BookingId,
                    CustomerId = request.CustomerId,
                    ProviderId = request.ProviderId,
                    TenantId = request.TenantId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    PaymentMethod = request.PaymentMethod,
                    TransactionId = Guid.NewGuid().ToString(),
                    PaymentGateway = "SimulatedGateway",
                    ProcessedAt = DateTime.UtcNow
                };
                
                await _publishEndpoint.Publish(paymentProcessedEvent);
                
                _logger.LogInformation($"PaymentProcessedEvent published for payment {paymentId}");
                
                return Ok(new { PaymentId = paymentId, Message = "Payment processed successfully" });
            }
            else
            {
                // Publish PaymentFailedEvent
                var paymentFailedEvent = new PaymentFailedEvent
                {
                    PaymentId = paymentId,
                    BookingId = request.BookingId,
                    CustomerId = request.CustomerId,
                    ProviderId = request.ProviderId,
                    TenantId = request.TenantId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    PaymentMethod = request.PaymentMethod,
                    FailureReason = "Simulated payment failure",
                    FailedAt = DateTime.UtcNow
                };
                
                await _publishEndpoint.Publish(paymentFailedEvent);
                
                _logger.LogInformation($"PaymentFailedEvent published for payment {paymentId}");
                
                return BadRequest(new { PaymentId = paymentId, Message = "Payment processing failed" });
            }
        }
    }
    
    public class ProcessPaymentRequest
    {
        public Guid BookingId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid ProviderId { get; set; }
        public Guid TenantId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string PaymentMethod { get; set; } = string.Empty; // CreditCard, PayPal, etc.
    }
}