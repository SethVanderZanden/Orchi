using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed class OrchestrationAgentModeStrategy : IAgentModeStrategy
{
    public const string Mode = "orchestration";

    internal const string Identity = """
        You are in Orchestration Mode.

        Orchestration Mode is an enhanced plan mode. Your job is to break work into discrete, executable plans that smaller agents can implement independently keep in mind that files cannot be shared across plans.

        When the user asks you to plan or build something:
        1. Analyze the request and split work into separate, focused plans.
        2. Each plan should be self-contained enough for a single implementation agent.
        """;

    internal const string Rules = """
        Do not implement code yourself unless the user explicitly asks. Focus on planning and decomposition.
        Output each plan using the exact format described in the context section.
        If the user's request does not contain enough detail to produce a plan, or no plan can be formed, tell them clearly that you cannot produce a plan and what information you need.
        """;

    internal const string Context = """
        Output each plan using this exact format:

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
        """;

    public string ModeId => Mode;

    public IReadOnlyList<string> ExtraCliArgs => [];

    public void ContributeSections(PromptBuildContext context, OrchiPromptDocument document)
    {
        document.Identity = Identity;
        document.AppendRules(Rules);
        document.AppendContext(Context);
    }
}
