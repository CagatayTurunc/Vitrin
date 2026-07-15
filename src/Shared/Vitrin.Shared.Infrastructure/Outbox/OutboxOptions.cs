namespace Vitrin.Shared.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollingIntervalMs { get; init; } = 1_000;
    public int BatchSize { get; init; } = 20;
    public int MaxRetryAttempts { get; init; } = 10;
    public int MaxBackoffSeconds { get; init; } = 300;
}
