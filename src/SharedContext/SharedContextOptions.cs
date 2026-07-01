namespace Orchi.SharedContext;

public sealed class SharedContextOptions
{
    public const string SectionName = "SharedContext";

    public string ConnectionString { get; set; } = "Data Source=orchi-context.db";

    public int MaxFilesPerIndex { get; set; } = 10_000;

    public TimeSpan IndexStaleAfter { get; set; } = TimeSpan.FromHours(1);

    public int RetrievalTopK { get; set; } = 8;

    public int RetrievalTokenBudget { get; set; } = 4000;
}
