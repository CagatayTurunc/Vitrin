using Microsoft.EntityFrameworkCore;
using Vitrin.Ai.Application.Commands;
using Vitrin.Ai.Domain.Entities;
using Vitrin.Ai.Infrastructure.Data;

namespace Vitrin.Ai.Infrastructure.Repositories;

public class AiRepository : IAiAnalysisRepository
{
    private readonly AiDbContext _context;

    public AiRepository(AiDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AiAnalysisResult analysisResult, CancellationToken cancellationToken)
    {
        await _context.AiAnalysisResults.AddAsync(analysisResult, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AiAnalysisResult?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await _context.AiAnalysisResults
            .AsNoTracking()
            .OrderByDescending(analysis => analysis.AnalyzedAt)
            .FirstOrDefaultAsync(a => a.ProductId == productId, cancellationToken);
    }
}
