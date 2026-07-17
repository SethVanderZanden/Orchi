using System.Text.Json;
using System.Text.Json.Serialization;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Caching;
using Orchi.Api.Infrastructure.Database;
using Orchi.Api.Infrastructure.Endpoints;
using Orchi.Api.Infrastructure.OpenApi;
using Orchi.Api.Infrastructure.Pipeline;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(10);
});

builder.Services
    .AddOrchiDatabase(builder.Configuration)
    .AddOrchiCaching(builder.Configuration)
    .AddOrchiPipeline(builder.Configuration)
    .AddOrchiAgents(builder.Configuration)
    .AddOrchiOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DesktopDev", policy =>
        policy.SetIsOriginAllowed(origin =>
                string.IsNullOrEmpty(origin) ||
                origin == "null" ||
                (Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                 uri.Host is "localhost" or "127.0.0.1"))
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

await app.ApplyOrchiMigrationsAsync();

app.UseCors("DesktopDev");
app.UseOrchiOpenApi();
app.MapOrchiEndpoints();

app.Run();

public partial class Program;
