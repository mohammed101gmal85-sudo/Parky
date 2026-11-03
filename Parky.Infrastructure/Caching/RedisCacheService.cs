using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parky.Application.Interfaces.Services;
using Parky.Infrastructure.Settings;
using StackExchange.Redis;

namespace Parky.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly RedisSettings _redisSettings;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IOptions<RedisSettings> redisSettings,
        IOptions<CacheOptions> cacheOptions,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _database = _redis.GetDatabase();
        _redisSettings = redisSettings.Value;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cachedValue = await _database
                .StringGetAsync(BuildKey(key))
                .WaitAsync(cancellationToken);

            if (cachedValue.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(
                cachedValue.ToString(),
                JsonOptions);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Redis cache read failed for key {CacheKey}",
                key);

            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (value is null)
            {
                return;
            }

            var cacheExpiration =
                expiration ?? _cacheOptions.DefaultExpiration;

            if (cacheExpiration <= TimeSpan.Zero)
            {
                _logger.LogWarning(
                    "Redis cache write skipped because expiration is invalid for key {CacheKey}",
                    key);

                return;
            }

            var serializedValue = JsonSerializer.Serialize(
                value,
                JsonOptions);

            await _database.StringSetAsync(
                BuildKey(key),
                serializedValue,
                cacheExpiration)
                .WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Redis cache write failed for key {CacheKey}",
                key);
        }
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _database
                .KeyDeleteAsync(BuildKey(key))
                .WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Redis cache removal failed for key {CacheKey}",
                key);
        }
    }

    public async Task RemoveByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(prefix);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullPrefix = BuildKey(prefix);

            foreach (var endpoint in _redis.GetEndPoints())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var server = _redis.GetServer(endpoint);

                if (!server.IsConnected || server.IsReplica)
                {
                    continue;
                }

                var keys = server.Keys(
                    database: _database.Database,
                    pattern: $"{fullPrefix}*",
                    pageSize: 500);

                foreach (var redisKey in keys)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await _database
                        .KeyDeleteAsync(redisKey)
                        .WaitAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Redis cache prefix removal failed for prefix {CachePrefix}",
                prefix);
        }
    }

    public async Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await _database
                .KeyExistsAsync(BuildKey(key))
                .WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Redis cache existence check failed for key {CacheKey}",
                key);

            return false;
        }
    }

    private string BuildKey(string key)
    {
        return $"{_redisSettings.InstanceName}{key}";
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "Cache key cannot be null or empty.",
                nameof(key));
        }
    }
}