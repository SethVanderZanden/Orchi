using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Orchi.Api.Tests.Common;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    public HttpClient CreateClientWithServices(Action<IServiceCollection>? configureServices = null)
    {
        if (configureServices is null)
        {
            return CreateClient();
        }

        return WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(configureServices);
        }).CreateClient();
    }
}

public static class HttpResponseExtensions
{
    public static async Task<T?> ReadJsonAsync<T>(this HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public static async Task<HttpStatusCode> GetStatusCodeAsync(this Task<HttpResponseMessage> responseTask)
    {
        using HttpResponseMessage response = await responseTask;
        return response.StatusCode;
    }
}
