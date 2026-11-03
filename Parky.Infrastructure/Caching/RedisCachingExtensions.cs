using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Parky.Application.Interfaces.Services;
using Parky.Infrastructure.Settings;
using StackExchange.Redis;

namespace Parky.Infrastructure.Caching;

public static class RedisCachingExtensions
{
    public static IServiceCollection AddRedisCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<RedisSettings>()
            .Bind(configuration.GetSection("Redis"))
            .Validate(
                settings =>
                    !string.IsNullOrWhiteSpace(settings.ConnectionString),
                "Redis ConnectionString is required.")
            .Validate(
                settings =>
                    !string.IsNullOrWhiteSpace(settings.InstanceName),
                "Redis InstanceName is required.")
            .ValidateOnStart();

        services
            .AddOptions<CacheOptions>()
            .Bind(configuration.GetSection("Cache"))
            .Validate(
                options => options.DefaultExpiration > TimeSpan.Zero,
                "Default cache expiration must be greater than zero.")
            .ValidateOnStart();
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var redisSettings = serviceProvider
                .GetRequiredService<IOptions<RedisSettings>>()
                .Value;

            var connectionOptions = ConfigurationOptions.Parse(
                redisSettings.ConnectionString);

            connectionOptions.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(connectionOptions);
        });

        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var redisSettings = serviceProvider
                .GetRequiredService<IOptions<RedisSettings>>()
                .Value;

            var connectionOptions = ConfigurationOptions.Parse(
                redisSettings.ConnectionString);

            connectionOptions.AbortOnConnectFail = false;
            connectionOptions.ConnectRetry = 3;
            connectionOptions.ConnectTimeout = 5000;
            connectionOptions.SyncTimeout = 5000;
            connectionOptions.AsyncTimeout = 5000;

            return ConnectionMultiplexer.Connect(connectionOptions);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}