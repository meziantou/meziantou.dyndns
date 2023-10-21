namespace Meziantou.DynDns;

internal sealed class CloudflareConfiguration
{
    public required string ZoneId { get; set; }
    public required string RecordName { get; set; }
    public required string ApiToken { get; set; }
}
