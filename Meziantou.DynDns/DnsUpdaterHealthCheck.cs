using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Meziantou.DynDns;

internal sealed class DnsUpdaterHealthCheck(IOptions<DynDnsConfiguration> options, DnsUpdaterState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var isHealthy = state.LastUpdatedAt != null && (DateTimeOffset.UtcNow - state.LastUpdatedAt) < (options.Value.UpdatePeriod + TimeSpan.FromMinutes(1));
        return Task.FromResult(isHealthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());
    }
}
