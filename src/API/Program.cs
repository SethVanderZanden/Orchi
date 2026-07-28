using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Attachments;
using Orchi.Api.Infrastructure.Caching;
using Orchi.Api.Infrastructure.Database;
using Orchi.Api.Infrastructure.Endpoints;
using Orchi.Api.Infrastructure.OpenApi;
using Orchi.Api.Infrastructure.Pipeline;
using Orchi.Api.Infrastructure.UserPreferences;

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

// Cap multipart / request body to attachment limits (+ small overhead) so oversized
// uploads cannot be buffered unboundedly before ChatAttachmentService rejects them.
long maxAttachmentBytes = builder.Configuration
    .GetSection(AttachmentOptions.SectionName)
    .GetValue("MaxFileSizeBytes", new AttachmentOptions().MaxFileSizeBytes);
long maxRequestBodyBytes = maxAttachmentBytes + (1024 * 1024);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestBodyBytes;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxRequestBodyBytes;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = 16_384;
});

builder.Services
    .AddOrchiDatabase(builder.Configuration)
    .AddOrchiCaching(builder.Configuration)
    .AddOrchiPipeline(builder.Configuration)
    .AddOrchiAgents(builder.Configuration)
    .AddOrchiUserPreferences()
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
await app.EnsureCodexBuiltInCatalogAsync();

app.UseCors("DesktopDev");
app.UseOrchiOpenApi();
app.MapOrchiEndpoints();

app.Run();

public partial class Program;
