namespace Orchi.Api.Infrastructure.Agents.Codex;

internal sealed class CodexAgentOptions
{
    public const string SectionName = "Agents:Codex";

    public string Executable { get; init; } = "codex";

    public string[] DefaultArgs { get; init; } =
    [
        "--skip-git-repo-check",
        "--sandbox",
        "workspace-write"
    ];

    public string[] AdditionalSearchPaths { get; init; } = [];

    public int TimeoutSeconds { get; init; } = 600;
}
