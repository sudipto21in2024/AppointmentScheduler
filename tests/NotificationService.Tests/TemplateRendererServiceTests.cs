using Xunit;
using System.Threading.Tasks;
using NotificationService.Services;
using Shared.DTOs.EmailTemplates;
using System.IO;
using System;

namespace NotificationService.Tests
{
    public class TemplateRendererServiceTests : IDisposable
    {
        private readonly TemplateRendererService _templateRendererService;
        private readonly string _templatesPath;
        private readonly string _originalCurrentDirectory;

        public TemplateRendererServiceTests()
        {
            _originalCurrentDirectory = Directory.GetCurrentDirectory();
            _templatesPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_templatesPath);
            Directory.SetCurrentDirectory(_templatesPath); // Set current directory for RazorLight to find templates

            // Create a dummy template file for testing
            File.WriteAllText(Path.Combine(_templatesPath, "TestTemplate.html"),
                "<h1>Hello, {{Name}}!</h1><p>This is a test for {{Value}}.</p>");
            
            // Initialize TemplateRendererService with the temporary path
            _templateRendererService = new TemplateRendererService();
        }

        [Fact]
        public async Task RenderTemplateAsync_WithValidModel_ReturnsRenderedContent()
        {
            // Arrange
            var model = new { Name = "World", Value = "RazorLight" };
            var templateName = "TestTemplate.html";

            // Act
            var result = await _templateRendererService.RenderTemplateAsync(templateName, model);

            // Assert
            Assert.Contains("<h1>Hello, World!</h1>", result);
            Assert.Contains("<p>This is a test for RazorLight.</p>", result);
        }

        [Fact]
        public async Task RenderTemplateAsync_WithBookingConfirmationDto_ReturnsRenderedContent()
        {
            // Arrange
            var model = new BookingConfirmationEmailDto
            {
                CustomerName = "John Doe",
                ServiceName = "Haircut",
                BookingDate = new DateTime(2025, 10, 26),
                BookingTime = "10:00 AM",
                ProviderName = "Jane Smith"
            };
            var templateName = "BookingConfirmation.html";

            // Create a dummy BookingConfirmation template file for testing
            File.WriteAllText(Path.Combine(_templatesPath, templateName),
                "Dear {{CustomerName}}, your booking for {{ServiceName}} on {{BookingDate}} at {{BookingTime}} with {{ProviderName}} is confirmed.");

            // Act
            var result = await _templateRendererService.RenderTemplateAsync(templateName, model);

            // Assert
            Assert.Contains("Dear John Doe, your booking for Haircut on 10/26/2025 at 10:00 AM with Jane Smith is confirmed.", result);
        }
        
        // Clean up the temporary directory after tests
        public void Dispose()
        {
            Directory.SetCurrentDirectory(_originalCurrentDirectory); // Restore original current directory
            if (Directory.Exists(_templatesPath))
            {
                Directory.Delete(_templatesPath, true);
            }
        }
    }
}