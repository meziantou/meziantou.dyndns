using System.Net;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Meziantou.DynDns;

internal sealed partial class CloudflareDnsUpdater : DnsUpdater
{
    private readonly ILogger<CloudflareDnsUpdater> _logger;
    private readonly CloudflareConfiguration _configuration;

    public CloudflareDnsUpdater(ILogger<CloudflareDnsUpdater> logger, IOptions<CloudflareConfiguration> configuration)
    {
        _logger = logger;
        _configuration = configuration.Value;
    }

    public override async Task UpdateAsync(IPAddress address, CancellationToken cancellationToken)
    {
        var record = await GetRecord(cancellationToken);
        await UpdateRecord(record, address, cancellationToken);
    }

    private async Task<ListDnsResponseEntry> GetRecord(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.cloudflare.com/client/v4/zones/{Uri.EscapeDataString(_configuration.ZoneId)}/dns_records?name={Uri.EscapeDataString(_configuration.RecordName)}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _configuration.ApiToken);

        using var response = await SharedHttpClient.Instance.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync(CloudflareGenerationContext.Default.ListDnsResponse, cancellationToken);

        if (result?.Result is [var item, ..])
            return item;

        throw new DynDnsException($"Cannot find record '{_configuration.RecordName}'");
    }

    private async Task UpdateRecord(ListDnsResponseEntry record, IPAddress address, CancellationToken cancellationToken)
    {
        var type = address.AddressFamily switch
        {
            System.Net.Sockets.AddressFamily.InterNetwork => "A",
            System.Net.Sockets.AddressFamily.InterNetworkV6 => "AAAA",
            _ => null,
        };

        if (type == null)
            return;

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"https://api.cloudflare.com/client/v4/zones/{Uri.EscapeDataString(_configuration.ZoneId)}/dns_records/{Uri.EscapeDataString(record.Id!)}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _configuration.ApiToken);

        var data = new UpdateEntryData
        {
            Type = type,
            Content = address.ToString(),
            Name = _configuration.RecordName,
        };
        request.Content = JsonContent.Create(data, CloudflareGenerationContext.Default.UpdateEntryData);

        using var response = await SharedHttpClient.Instance.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            Log.CouldNotUpdateRecord(_logger, error);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Error, Message = "Cannot update record: {Message}")]
        public static partial void CouldNotUpdateRecord(ILogger logger, string message);
    }

    [JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNameCaseInsensitive = false)]
    [JsonSerializable(typeof(ListDnsResponse))]
    [JsonSerializable(typeof(ListDnsResponseEntry))]
    [JsonSerializable(typeof(UpdateEntryData))]
    private sealed partial class CloudflareGenerationContext : JsonSerializerContext
    {
    }

    private sealed class ListDnsResponse
    {
        [JsonPropertyName("messages")]
        public JsonNode? Messages { get; set; }

        [JsonPropertyName("errors")]
        public JsonNode? Errors { get; set; }

        [JsonPropertyName("result")]
        public ListDnsResponseEntry[]? Result { get; set; }
    }

    private sealed class ListDnsResponseEntry
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }

        [JsonPropertyName("ttl")]
        public long? TimeToLive { get; set; }

        [JsonPropertyName("proxied")]
        public bool? Proxied { get; set; }

        public override string ToString() => $"{Name} ({Type}): {Content}";
    }

    private sealed class UpdateEntryData
    {
        [JsonPropertyName("type")]
        public required string? Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("content")]
        public required string? Content { get; set; }

        [JsonPropertyName("ttl")]
        public long? TimeToLive { get; set; }

        [JsonPropertyName("proxied")]
        public bool? Proxied { get; set; }
    }
}
