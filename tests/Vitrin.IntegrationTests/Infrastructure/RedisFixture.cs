using Testcontainers.Redis;

namespace Vitrin.IntegrationTests.Infrastructure;

public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RedisCollection : ICollectionFixture<RedisFixture>
{
    public const string Name = "redis-integration";
}
