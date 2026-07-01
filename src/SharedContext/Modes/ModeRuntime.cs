namespace Orchi.SharedContext.Modes;

internal sealed class ModeRuntime : IModeRuntime
{
    private static readonly IReadOnlyDictionary<string, CursorCliProfile> Profiles =
        new Dictionary<string, CursorCliProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["agent"] = new CursorCliProfile(CursorCliProfileKind.Agent, []),
            ["implement"] = new CursorCliProfile(CursorCliProfileKind.Agent, []),
            ["plan"] = new CursorCliProfile(CursorCliProfileKind.Plan, ["--mode=plan"]),
            ["orchestrate"] = new CursorCliProfile(CursorCliProfileKind.Plan, ["--mode=plan"]),
            ["goal"] = new CursorCliProfile(CursorCliProfileKind.Plan, ["--mode=plan"]),
            ["participant"] = new CursorCliProfile(CursorCliProfileKind.Ask, ["--mode=ask"])
        };

    public CursorCliProfile ResolveCliProfile(string modeKey)
    {
        if (Profiles.TryGetValue(modeKey, out CursorCliProfile? profile))
        {
            return profile;
        }

        return Profiles["agent"];
    }

    public bool ShouldPreserveResume(string previousModeKey, string newModeKey)
    {
        if (string.Equals(previousModeKey, newModeKey, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        CursorCliProfile previous = ResolveCliProfile(previousModeKey);
        CursorCliProfile next = ResolveCliProfile(newModeKey);
        return previous.Kind == next.Kind;
    }

    public ModeTransitionContext? BuildTransitionContext(
        string? previousModeKey,
        string newModeKey,
        DateTimeOffset? modeChangedAt)
    {
        if (string.IsNullOrWhiteSpace(previousModeKey) ||
            string.Equals(previousModeKey, newModeKey, StringComparison.OrdinalIgnoreCase) ||
            modeChangedAt is null)
        {
            return null;
        }

        return new ModeTransitionContext(previousModeKey, newModeKey, modeChangedAt.Value);
    }
}
