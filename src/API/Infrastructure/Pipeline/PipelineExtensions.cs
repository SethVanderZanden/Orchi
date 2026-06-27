using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Behaviours;

namespace Orchi.Api.Infrastructure.Pipeline;

public static class PipelineExtensions
{
    public static IServiceCollection AddOrchiPipeline(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PerformanceOptions>(configuration.GetSection(PerformanceOptions.SectionName));

        services.Scan(scan => scan
            .FromAssemblyOf<Program>()
            .AddClasses(classes => classes
                    .AssignableTo(typeof(IQueryHandler<,>))
                    .Where(type => type.Namespace?.Contains(".Common.Behaviours") != true),
                publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes
                    .AssignableTo(typeof(ICommandHandler<>))
                    .Where(type => type.Namespace?.Contains(".Common.Behaviours") != true),
                publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes
                    .AssignableTo(typeof(ICommandHandler<,>))
                    .Where(type => type.Namespace?.Contains(".Common.Behaviours") != true),
                publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        services.AddValidatorsFromAssemblyContaining<Program>();

        TryDecorateOpenGeneric(services, typeof(IQueryHandler<,>), typeof(ValidationBehaviour.QueryHandler<,>));
        TryDecorateOpenGeneric(services, typeof(ICommandHandler<,>), typeof(ValidationBehaviour.CommandHandler<,>));
        TryDecorateOpenGeneric(services, typeof(ICommandHandler<>), typeof(ValidationBehaviour.CommandBaseHandler<>));

        TryDecorateOpenGeneric(services, typeof(IQueryHandler<,>), typeof(LoggingBehaviour.QueryHandler<,>));
        TryDecorateOpenGeneric(services, typeof(ICommandHandler<,>), typeof(LoggingBehaviour.CommandHandler<,>));
        TryDecorateOpenGeneric(services, typeof(ICommandHandler<>), typeof(LoggingBehaviour.CommandBaseHandler<>));

        TryDecorateOpenGeneric(services, typeof(IQueryHandler<,>), typeof(PerformanceBehaviour.QueryHandler<,>));
        TryDecorateOpenGeneric(services, typeof(ICommandHandler<,>), typeof(PerformanceBehaviour.CommandHandler<,>));
        TryDecorateOpenGeneric(services, typeof(ICommandHandler<>), typeof(PerformanceBehaviour.CommandBaseHandler<>));

        return services;
    }

    private static void TryDecorateOpenGeneric(IServiceCollection services, Type serviceType, Type decoratorType)
    {
        if (HasOpenGenericImplementations(services, serviceType))
        {
            services.Decorate(serviceType, decoratorType);
        }
    }

    private static bool HasOpenGenericImplementations(IServiceCollection services, Type openGenericServiceType)
    {
        return services.Any(descriptor =>
            descriptor.ServiceType.IsGenericType &&
            descriptor.ServiceType.GetGenericTypeDefinition() == openGenericServiceType);
    }
}
