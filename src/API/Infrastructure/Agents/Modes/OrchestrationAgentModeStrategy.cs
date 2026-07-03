namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed class OrchestrationAgentModeStrategy : IAgentModeStrategy
{
    public const string Mode = "orchestration";

    internal const string Instructions = """
        You are in Orchestration Mode.

        Orchestration Mode is an enhanced plan mode. Your job is to break work into discrete, executable plans that smaller agents can implement independently.

        When the user asks you to plan or build something:
        1. Analyze the request and split work into separate, focused plans.
        2. Each plan should be self-contained enough for a single implementation agent.
        3. Output each plan using this exact format:

        <!-- orchi-plan:kebab-case-id -->
        # Plan Title

        ## Summary
        Brief description of what this plan accomplishes.

        ## Tasks
        - [ ] Task one
        - [ ] Task two

        ## Implementation notes
        Relevant context, constraints, and file paths.

        <!-- /orchi-plan -->

        Do not implement code yourself unless the user explicitly asks. Focus on planning and decomposition.
        """;

    public string ModeId => Mode;

    public IReadOnlyList<string> ExtraCliArgs => [];

    public string BuildPrompt(string userContent) =>
        $"{Instructions}\n\n---\n\nUser message:\n{userContent}";
}
