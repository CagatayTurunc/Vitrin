using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Vitrin.Shared.Infrastructure.Redis;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
    Task InvalidateAsync(string key);
    Task InvalidatePatternAsync(string pattern);
}

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;
    
    public RedisCacheService(
        IConnectionMultiplexer redis, 
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _logger = logger;
    }
    
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            
            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }
            
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
            return default;
        }
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, serialized, expiration);
            _logger.LogDebug("Cache set for key: {Key} with expiration: {Expiration}", 
                key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
        }
    }
    
    public async Task InvalidateAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
        _logger.LogDebug("Cache invalidated for key: {Key}", key);
    }
    
    public async Task InvalidatePatternAsync(string pattern)
    {
        var endpoints = _redis.GetEndPoints();
        if (endpoints.Length > 0)
        {
            var server = _redis.GetServer(endpoints.First());
            var keys = server.Keys(pattern: pattern);
            
            foreach (var key in keys)
            {
                await _db.KeyDeleteAsync(key);
            }
            
            _logger.LogDebug("Cache invalidated for pattern: {Pattern}", pattern);
        }
    }
}
