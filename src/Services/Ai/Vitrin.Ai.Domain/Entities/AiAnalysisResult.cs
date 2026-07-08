using Vitrin.Shared.Kernel.Domain;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Ai.Domain.Entities;

public class AiAnalysisResult : AggregateRoot
{
    public Guid ProductId { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string Tags { get; private set; } = string.Empty; // Comma separated tags
    public DateTime AnalyzedAt { get; private set; }

    private AiAnalysisResult() { } // EF Core

    public static Result<AiAnalysisResult> Create(Guid productId, string summary, string tags)
    {
        var result = new AiAnalysisResult
        {
            ProductId = productId,
            Summary = summary,
            Tags = tags,
            AnalyzedAt = DateTime.UtcNow
        };

        return Result<AiAnalysisResult>.Success(result);
    }
}
