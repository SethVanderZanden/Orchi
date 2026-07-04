namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public sealed class PromptBuildContext
{
    public required string ModeId { get; init; }

    public required string UserContent { get; init; }

    public required string WorkspacePath { get; init; }

    public string? PlanFilePath { get; init; }

    public Guid? ParentChatId { get; init; }

    public bool IsFirstUserTurn { get; init; }
}
