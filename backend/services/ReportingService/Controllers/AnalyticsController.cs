using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ReportingService.Services;
using Shared.DTOs;

namespace ReportingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Gets booking analytics data aggregated by time periods
        /// </summary>
        /// <param name="providerId">Service provider ID</param>
        /// <param name="filter">Analytics filter parameters</param>
        /// <returns>Booking analytics data</returns>
        [HttpGet("bookings")]
        public async Task<ActionResult<BookingAnalyticsDto>> GetBookingAnalytics(
            [FromQuery] Guid providerId,
            [FromQuery] AnalyticsFilterDto filter)
        {
            try
            {
                // Get tenant ID from claims (in a real implementation)
                var tenantId = GetTenantId();

                var analytics = await _analyticsService.GetBookingDataAsync(providerId, tenantId, filter);
                return Ok(analytics);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving booking analytics data", Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets revenue analytics data including earnings and commission tracking
        /// </summary>
        /// <param name="providerId">Service provider ID</param>
        /// <param name="filter">Analytics filter parameters</param>
        /// <returns>Revenue analytics data</returns>
        [HttpGet("revenue")]
        public async Task<ActionResult<RevenueAnalyticsDto>> GetRevenueAnalytics(
            [FromQuery] Guid providerId,
            [FromQuery] AnalyticsFilterDto filter)
        {
            try
            {
                // Get tenant ID from claims (in a real implementation)
                var tenantId = GetTenantId();

                var analytics = await _analyticsService.GetRevenueDataAsync(providerId, tenantId, filter);
                return Ok(analytics);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving revenue analytics data", Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets customer insights including booking history and feedback
        /// </summary>
        /// <param name="providerId">Service provider ID</param>
        /// <param name="filter">Analytics filter parameters</param>
        /// <returns>Customer insights data</returns>
        [HttpGet("customers")]
        public async Task<ActionResult<CustomerInsightsDto>> GetCustomerInsights(
            [FromQuery] Guid providerId,
            [FromQuery] AnalyticsFilterDto filter)
        {
            try
            {
                // Get tenant ID from claims (in a real implementation)
                var tenantId = GetTenantId();

                var insights = await _analyticsService.GetCustomerDataAsync(providerId, tenantId, filter);
                return Ok(insights);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving customer insights data", Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets tenant ID from claims (placeholder implementation)
        /// </summary>
        /// <returns>Tenant ID</returns>
        private Guid GetTenantId()
        {
            // In a real implementation, this would extract the tenant ID from the user's claims
            // For now, we'll return a default GUID
            return Guid.NewGuid();
        }
    }
}