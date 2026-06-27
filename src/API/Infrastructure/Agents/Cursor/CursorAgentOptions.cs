namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal sealed class CursorAgentOptions
{
    public const string SectionName = "Agents:Cursor";

    public string Executable { get; init; } = "agent";

    public string[] DefaultArgs { get; init; } = ["--force", "--trust"];

    public string[] AdditionalSearchPaths { get; init; } = [];

    public int TimeoutSeconds { get; init; } = 600;
}
