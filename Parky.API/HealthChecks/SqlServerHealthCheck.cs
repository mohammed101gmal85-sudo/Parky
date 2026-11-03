
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Parky.API.HealthChecks;

public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    private const string ConnectionStringName = "DefaultConnection";

    public SqlServerHealthCheck(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString(
            ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy(
                "Database connection string is not configured.");
        }

        try
        {
            await using var connection = new SqlConnection(
                connectionString);

            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();

            command.CommandText = "SELECT 1";

            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy(
                "SQL Server is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "SQL Server health check failed.",
                exception);
        }
    }
}
