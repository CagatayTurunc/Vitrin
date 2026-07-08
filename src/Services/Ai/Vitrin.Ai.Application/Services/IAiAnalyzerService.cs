namespace Vitrin.Ai.Application.Services;

public interface IAiAnalyzerService
{
    Task<(string Summary, string[] Tags)> AnalyzeProductTextAsync(string name, string description, CancellationToken cancellationToken);
}
