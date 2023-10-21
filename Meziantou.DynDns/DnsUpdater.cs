using System.Net;

namespace Meziantou.DynDns;

internal abstract class DnsUpdater
{
    public abstract Task UpdateAsync(IPAddress address, CancellationToken cancellationToken);
}
