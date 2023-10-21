namespace Meziantou.DynDns;

internal sealed class DynDnsConfiguration
{
    public TimeSpan UpdatePeriod { get; set; } = TimeSpan.FromMinutes(5);
}
