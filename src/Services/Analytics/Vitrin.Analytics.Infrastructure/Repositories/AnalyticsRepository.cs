using Vitrin.Analytics.Application.Commands;
using Vitrin.Analytics.Domain.Entities;
using Vitrin.Analytics.Infrastructure.Data;

namespace Vitrin.Analytics.Infrastructure.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AnalyticsDbContext _context;

    public AnalyticsRepository(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken)
    {
        await _context.AnalyticsEvents.AddAsync(analyticsEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
