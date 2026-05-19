using Microsoft.Extensions.Options;
using ProyectMVP.DeviceAgent;
using ProyectMVP.DeviceAgent.Lpr;
using ProyectMVP.DeviceAgent.Options;
using ProyectMVP.DeviceAgent.Outbox;
using ProyectMVP.DeviceAgent.Sync;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<DeviceAgentOptions>(
    builder.Configuration.GetSection(DeviceAgentOptions.SectionName));

builder.Services.AddSingleton<ISightingOutbox, SqliteSightingOutbox>();
builder.Services.AddSingleton<ILprReader, DemoLprReader>();

builder.Services.AddHttpClient<ICentralApiClient, CentralApiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<DeviceAgentOptions>>().Value;
    client.BaseAddress = new Uri(options.ApiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
