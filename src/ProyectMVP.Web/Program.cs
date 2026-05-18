using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5290";

builder.Services.AddHttpClient("ProyectMVP.Api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.Map("/proxy/{**path}", async (string path, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("ProyectMVP.Api");
    var targetUri = new Uri(client.BaseAddress!, path + context.Request.QueryString);

    using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri);

    if (context.Request.ContentLength > 0)
    {
        request.Content = new StreamContent(context.Request.Body);
        if (!string.IsNullOrEmpty(context.Request.ContentType))
        {
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
        }
    }

    foreach (var header in context.Request.Headers)
    {
        if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
        {
            request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

    context.Response.StatusCode = (int)response.StatusCode;
    foreach (var header in response.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }
    foreach (var header in response.Content.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }

    context.Response.Headers.Remove("transfer-encoding");
    await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
});

app.MapGet("/api/config", (IConfiguration config) => Results.Ok(new
{
    apiBaseUrl = config["ApiBaseUrl"],
    proxyPrefix = "/proxy"
}));

app.Run();
