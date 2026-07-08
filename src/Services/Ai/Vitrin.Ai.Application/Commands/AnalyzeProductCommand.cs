using Vitrin.Ai.Application.Services;
using Vitrin.Ai.Domain.Entities;
using Vitrin.Shared.Kernel.Results;
using MediatR;

namespace Vitrin.Ai.Application.Commands;

public record AnalyzeProductCommand(Guid ProductId, string ProductName, string ProductDescription) : IRequest<Result<Guid>>;

public interface IAiAnalysisRepository
{
    Task AddAsync(AiAnalysisResult analysisResult, CancellationToken cancellationToken);
    Task<AiAnalysisResult?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);
}

public class AnalyzeProductCommandHandler : IRequestHandler<AnalyzeProductCommand, Result<Guid>>
{
    private readonly IAiAnalyzerService _aiService;
    private readonly IAiAnalysisRepository _repository;

    public AnalyzeProductCommandHandler(IAiAnalyzerService aiService, IAiAnalysisRepository repository)
    {
        _aiService = aiService;
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(AnalyzeProductCommand request, CancellationToken cancellationToken)
    {
        // Analiz zaten yapılmış mı kontrol edilebilir (şimdilik her seferinde yapalım)
        var (summary, tags) = await _aiService.AnalyzeProductTextAsync(request.ProductName, request.ProductDescription, cancellationToken);
        
        var tagsString = string.Join(",", tags);
        var resultEntity = AiAnalysisResult.Create(request.ProductId, summary, tagsString);
        
        if (!resultEntity.IsSuccess)
            return Result<Guid>.Failure(resultEntity.Error);

        await _repository.AddAsync(resultEntity.Value, cancellationToken);
        
        return Result<Guid>.Success(resultEntity.Value.Id);
    }
}
