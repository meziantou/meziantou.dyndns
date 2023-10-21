using Meziantou.DynDns;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<CloudflareConfiguration>(builder.Configuration.GetSection("Cloudflare"));
builder.Services.AddSingleton<DnsUpdater, CloudflareDnsUpdater>();
builder.Services.AddHostedService<DynDnsService>();
builder.Services.AddHealthChecks();
builder.Services.Configure<HostOptions>(options => options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost);

var host = builder.Build();
host.UseHealthChecks("/health");

await host.RunAsync();
