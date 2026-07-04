namespace Orchi.Api.Infrastructure.Caching;

public sealed class OrchiCacheOptions
{
    public const string SectionName = "Cache";

    public int DefaultExpirationMinutes { get; init; } = 5;

    public int WorkspaceDiffExpirationSeconds { get; init; } = 30;

    public int CursorExecutableExpirationMinutes { get; init; } = 60;

    public int PlanExpirationMinutes { get; init; } = 10;

    public int AgentModelsExpirationMinutes { get; init; } = 1440;

    public DistributedCacheOptions Distributed { get; init; } = new();
}

public sealed class DistributedCacheOptions
{
    public bool Enabled { get; init; }

    public string Provider { get; init; } = "None";

    public string ConnectionString { get; init; } = "";
}
