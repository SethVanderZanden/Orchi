namespace Orchi.Api.Common.Behaviours;

internal sealed class PerformanceOptions
{
    public const string SectionName = "Performance";

    public int SlowQueryThresholdMs { get; init; } = 500;

    public int SlowCommandThresholdMs { get; init; } = 1000;
}
