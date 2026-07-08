using Vitrin.Ai.Application.Services;

namespace Vitrin.Ai.Infrastructure.Services;

public class MockAiAnalyzerService : IAiAnalyzerService
{
    public Task<(string Summary, string[] Tags)> AnalyzeProductTextAsync(string name, string description, CancellationToken cancellationToken)
    {
        // Mock implementation
        var fakeSummary = $"Bu ürün ({name}), yapay zeka tarafından analiz edildi. Açıklaması: {description.Substring(0, Math.Min(description.Length, 50))}...";
        var fakeTags = new[] { "inovasyon", "teknoloji", "yeni" };

        return Task.FromResult((fakeSummary, fakeTags));
    }
}
