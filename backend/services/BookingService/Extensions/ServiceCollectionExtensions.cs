using Microsoft.Extensions.DependencyInjection;
using BookingService.Services;

namespace BookingService.Extensions
{
    /// <summary>
    /// Extension methods for configuring services in the BookingService
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds booking services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddBookingServices(this IServiceCollection services)
        {
            // Register the booking service interface
            // Note: The implementation will be registered in a separate extension method
            // or when the concrete implementation is created
            // services.AddScoped<IBookingService, BookingService>();
            
            return services;
        }
    }
}