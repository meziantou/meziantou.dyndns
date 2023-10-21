using System.Net;

namespace Meziantou.DynDns;
internal sealed partial class DynDnsService : BackgroundService
{
    private static readonly string[] IpServiceUrls = ["https://ipinfo.io/ip", "https://api.ipify.org/", "https://api.infoip.io/ip"];

    private readonly ILogger<DynDnsService> _logger;
    private readonly IEnumerable<DnsUpdater> _dnsUpdaters;

    public DynDnsService(ILogger<DynDnsService> logger, IEnumerable<DnsUpdater> dnsUpdaters)
    {
        _logger = logger;
        _dnsUpdaters = dnsUpdaters;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var parallelOptions = new ParallelOptions { CancellationToken = stoppingToken };

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            var address = await GetIpAddressAsync(stoppingToken);
            if (address != null)
            {
                await Parallel.ForEachAsync(_dnsUpdaters, parallelOptions, async (dns, cancellationToken) => await dns.UpdateAsync(address, cancellationToken));
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task<IPAddress?> GetIpAddressAsync(CancellationToken cancellationToken)
    {
        foreach (var url in IpServiceUrls)
        {
            try
            {
                var address = await SharedHttpClient.Instance.GetStringAsync(url, cancellationToken);
                if (IPAddress.TryParse(address, out var ipAddress))
                    return ipAddress;
            }
            catch (Exception ex)
            {
                Log.CouldNotGetIpAddress(_logger, ex, url);
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
