using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orchi.SharedContext.Events;
using Orchi.SharedContext.Indexing;
using Orchi.SharedContext.Modes;
using Orchi.SharedContext.Prompts;
using Orchi.SharedContext.Session;
using Orchi.SharedContext.Storage;
using Orchi.SharedContext.Vectors;

namespace Orchi.SharedContext;

public static class SharedContextExtensions
{
    public static IServiceCollection AddOrchiSharedContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SharedContextOptions>(configuration.GetSection(SharedContextOptions.SectionName));

        string connectionString = configuration.GetSection(SharedContextOptions.SectionName)["ConnectionString"]
            ?? "Data Source=orchi-context.db";

        services.AddDbContextFactory<SharedContextDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddSingleton<IContextStore, EfContextStore>();
        services.AddSingleton<IProjectIndexer, ProjectIndexer>();
        services.AddSingleton<IVectorStore, KeywordVectorStore>();
        services.AddSingleton<IEmbeddingProvider, NullEmbeddingProvider>();
        services.AddSingleton<IModeRuntime, ModeRuntime>();
        services.AddSingleton<ProjectRulesLoader>();
        services.AddSingleton<IPromptBuilder, PromptBuilder>();
        services.AddSingleton<ISessionDistiller, SessionDistiller>();
        services.AddSingleton<IWorkspaceEventBus, InProcessWorkspaceEventBus>();
        services.AddHostedService<WorkspaceIndexWorker>();

        return services;
    }

    public static async Task ApplySharedContextMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        IDbContextFactory<SharedContextDbContext> factory =
            scope.ServiceProvider.GetRequiredService<IDbContextFactory<SharedContextDbContext>>();

        await using SharedContextDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
