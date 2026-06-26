using Scalar.AspNetCore;

namespace Orchi.Api.Infrastructure.OpenApi;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOrchiOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi();

        return services;
    }

    public static WebApplication UseOrchiOpenApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.WithTitle("Orchi API");
            });

            app.MapGet("/", () => Results.Redirect("/scalar/v1"))
                .ExcludeFromDescription();
        }

        return app;
    }
}
