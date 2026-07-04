using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class AgentPromptComposerTests
{
    private readonly AgentPromptComposer _composer = PromptTestHelpers.CreateComposer();

    [Fact]
    public void Compose_DefaultMode_WrapsMessageInOrchiEnvelope()
    {
        const string userContent = "Build a login page";
        var session = PromptTestHelpers.CreateSession();

        string prompt = _composer.Compose(session, userContent);

        Assert.StartsWith("<orchi>", prompt);
        Assert.EndsWith("</orchi>", prompt);
        Assert.Contains($"<message>{userContent}</message>", prompt);
        Assert.Contains(GlobalPromptRules.MetaRule.Trim(), prompt);
        Assert.DoesNotContain("<identity>", prompt);
        Assert.DoesNotContain("<task>", prompt);
    }

    [Fact]
    public void Compose_OrchestrationMode_IncludesModeSectionsAndMessage()
    {
        const string userContent = "Plan a refactor";
        var session = PromptTestHelpers.CreateSession(OrchestrationAgentModeStrategy.Mode);

        string prompt = _composer.Compose(session, userContent);

        Assert.StartsWith("<orchi>", prompt);
        Assert.Contains("<identity>", prompt);
        Assert.Contains("You are in Orchestration Mode.", prompt);
        Assert.Contains("<rules>", prompt);
        Assert.Contains("Do not implement code yourself", prompt);
        Assert.Contains(GlobalPromptRules.MetaRule.Trim(), prompt);
        Assert.Contains("<context>", prompt);
        Assert.Contains("<!-- orchi-plan:kebab-case-id -->", prompt);
        Assert.Contains($"<message>{userContent}</message>", prompt);
        Assert.DoesNotContain("---", prompt);
        Assert.DoesNotContain("User message:", prompt);
    }

    [Fact]
    public void Compose_WithPlanFilePath_IncludesTaskSection()
    {
        const string planPath = ".orchi/plan-auth.md";
        var session = PromptTestHelpers.CreateSession(planFilePath: planPath);

        string prompt = _composer.Compose(session, "Start with the login form");

        Assert.Contains($"<task>Implement the plan at `{planPath}`.", prompt);
        Assert.Contains($"delete `{planPath}`", prompt);
        Assert.Contains("<message>Start with the login form</message>", prompt);
    }

    [Fact]
    public void Compose_WithPlanFilePath_OnFollowUpTurn_OmitsTaskSection()
    {
        const string planPath = ".orchi/plan-auth.md";
        var session = PromptTestHelpers.CreateSession(
            planFilePath: planPath,
            messages:
            [
                new ChatMessage(Guid.NewGuid(), "user", "Begin implementation.", DateTimeOffset.UtcNow),
                new ChatMessage(Guid.NewGuid(), "assistant", "Working on it.", DateTimeOffset.UtcNow),
            ]);

        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "user", "Continue.", DateTimeOffset.UtcNow));

        string prompt = _composer.Compose(session, "Continue.");

        Assert.DoesNotContain("<task>", prompt);
        Assert.Contains("<message>Continue.</message>", prompt);
    }

    [Fact]
    public void Compose_ImplementationMode_IncludesScopedRules()
    {
        const string planPath = ".orchi/plan-auth.md";
        var session = PromptTestHelpers.CreateSession(
            ImplementationAgentModeStrategy.Mode,
            planFilePath: planPath);

        string prompt = _composer.Compose(session, "Begin implementation.");

        Assert.Contains("scoped plan file", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scope boundary", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"<task>Implement the plan at `{planPath}`.", prompt);
    }

    [Fact]
    public void Compose_WithParentChatId_IncludesParentInContext()
    {
        var parentChatId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var session = PromptTestHelpers.CreateSession(parentChatId: parentChatId);

        string prompt = _composer.Compose(session, "Continue the plan");

        Assert.Contains($"Parent chat: {parentChatId}", prompt);
    }

    [Fact]
    public void GetExtraCliArgs_DefaultMode_ReturnsEmpty()
    {
        IReadOnlyList<string> args = _composer.GetExtraCliArgs(DefaultAgentModeStrategy.Mode);

        Assert.Empty(args);
    }
}
