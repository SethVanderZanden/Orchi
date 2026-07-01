namespace Orchi.SharedContext.Modes;

public enum CursorCliProfileKind
{
    Agent,
    Plan,
    Ask
}

public sealed record CursorCliProfile(CursorCliProfileKind Kind, IReadOnlyList<string> ExtraArgs);

public sealed record ModeTransitionContext(string PreviousModeKey, string NewModeKey, DateTimeOffset ChangedAt);

public interface IModeRuntime
{
    CursorCliProfile ResolveCliProfile(string modeKey);

    bool ShouldPreserveResume(string previousModeKey, string newModeKey);

    ModeTransitionContext? BuildTransitionContext(
        string? previousModeKey,
        string newModeKey,
        DateTimeOffset? modeChangedAt);
}
