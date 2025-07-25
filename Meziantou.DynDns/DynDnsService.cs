using System.Net;
using Microsoft.Extensions.Options;

namespace Meziantou.DynDns;
internal sealed partial class DynDnsService(ILogger<DynDnsService> logger, IEnumerable<DnsUpdater> dnsUpdaters, IOptions<DynDnsConfiguration> configuration, DnsUpdaterState state, HttpClient httpClient) : BackgroundService
{
    private static readonly string[] IpServiceUrls = ["https://ipinfo.io/ip", "https://api.ipify.org/", "https://api.infoip.io/ip"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var parallelOptions = new ParallelOptions { CancellationToken = stoppingToken };

        using var timer = new PeriodicTimer(configuration.Value.UpdatePeriod);
        do
        {
            var address = await GetIpAddressAsync(stoppingToken);
            if (address != null)
            {
                await Parallel.ForEachAsync(dnsUpdaters, parallelOptions, async (dns, cancellationToken) => await dns.UpdateAsync(address, cancellationToken));
                state.LastUpdatedAt = DateTimeOffset.UtcNow;
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task<IPAddress?> GetIpAddressAsync(CancellationToken cancellationToken)
    {
        foreach (var url in IpServiceUrls)
        {
            try
            {
                var address = await httpClient.GetStringAsync(url, cancellationToken);
                if (IPAddress.TryParse(address, out var ipAddress))
                    return ipAddress;
            }
            catch (Exception ex)
            {
                Log.CouldNotGetIpAddress(logger, ex, url);
            }
        }

        return null;
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get IP address from '{URL}'")]
        public static partial void CouldNotGetIpAddress(ILogger logger, Exception ex, string url);
    }
}
