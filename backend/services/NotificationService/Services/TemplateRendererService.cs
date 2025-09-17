using System.IO;
using System.Threading.Tasks;
using RazorLight; // Using RazorLight for template rendering

namespace NotificationService.Services
{
    public class TemplateRendererService : ITemplateRendererService
    {
        private readonly RazorLightEngine _razorEngine;

        public TemplateRendererService()
        {
            _razorEngine = new RazorLightEngineBuilder()
                .UseFileSystemProject(Path.Combine(Directory.GetCurrentDirectory(), "Templates")) // Adjust path as needed
                .UseMemoryCachingProvider()
                .Build();
        }

        public async Task<string> RenderTemplateAsync<T>(string templateName, T model)
        {
            // RazorLight expects templateName without file extension
            var templateKey = Path.GetFileNameWithoutExtension(templateName);
            return await _razorEngine.CompileRenderAsync(templateKey, model);
        }
    }
}