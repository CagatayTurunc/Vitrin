namespace Vitrin.Ai.Infrastructure.Services;

public sealed class AiQuotaOptions
{
    public const string SectionName = "AiQuota";

    public int DailyRequestLimit { get; init; } = 10;
}
