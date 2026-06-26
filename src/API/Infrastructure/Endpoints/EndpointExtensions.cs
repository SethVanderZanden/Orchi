using System.Reflection;
using Orchi.Api.Common.Abstractions;

namespace Orchi.Api.Infrastructure.Endpoints;

public static class EndpointExtensions
{
    public static WebApplication MapOrchiEndpoints(this WebApplication app)
    {
        IEnumerable<IEndpoint> endpoints = Assembly.GetExecutingAssembly()
            .DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false } &&
                        typeof(IEndpoint).IsAssignableFrom(t))
            .Select(Activator.CreateInstance)
            .Cast<IEndpoint>();

        foreach (IEndpoint endpoint in endpoints)
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
