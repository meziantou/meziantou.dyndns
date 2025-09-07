using Meziantou.AspNetCore.ServiceDefaults;
using Meziantou.DynDns;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<DynDnsConfiguration>(builder.Configuration.GetSection("DynDns"));
builder.Services.Configure<CloudflareConfiguration>(builder.Configuration.GetSection("Cloudflare"));
builder.Services.AddSingleton<DnsUpdater, CloudflareDnsUpdater>();
builder.Services.AddSingleton<DnsUpdaterState>();
builder.Services.AddHostedService<DynDnsService>();
builder.Services.AddHealthChecks()
    .AddTypeActivatedCheck<DnsUpdaterHealthCheck>("DnsUpdater");
builder.Services.Configure<HostOptions>(options => options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost);

var host = builder.Build();
await host.RunAsync();
