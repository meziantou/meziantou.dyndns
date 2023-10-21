using Meziantou.DynDns;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
        listenOptions.UseHttps();
    });
});

builder.Services.Configure<DynDnsConfiguration>(builder.Configuration.GetSection("DynDns"));
builder.Services.Configure<CloudflareConfiguration>(builder.Configuration.GetSection("Cloudflare"));
builder.Services.AddSingleton<DnsUpdater, CloudflareDnsUpdater>();
builder.Services.AddHostedService<DynDnsService>();
builder.Services.AddHealthChecks();
builder.Services.Configure<HostOptions>(options => options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost);

var host = builder.Build();
host.UseHealthChecks("/health");

await host.RunAsync();
