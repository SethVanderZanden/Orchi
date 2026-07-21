using Microsoft.Extensions.Options;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Codex;

namespace Orchi.Api.Tests.Infrastructure.Agents.Codex;

public class CodexNdjsonParserTests
{
    private static CodexCliArgumentBuilder CreateArgumentBuilder(CodexAgentOptions? options = null) =>
        new(Options.Create(options ?? new CodexAgentOptions()));
    [Fact]
    public void ParseLine_ThreadStarted_ReturnsSessionStartedEvent()
    {
        const string line = """
            {"type":"thread.started","thread_id":"codex-thread-1"}
            """;

        var parser = new CodexNdjsonParser();
        AgentSessionStartedEvent started =
            Assert.IsType<AgentSessionStartedEvent>(Assert.Single(parser.ParseLine(line)));
        Assert.Equal("codex-thread-1", started.ExternalSessionId);
    }

    [Fact]
    public void ParseLine_AgentMessageCompleted_ReturnsTextDelta()
    {
        const string line = """
            {"type":"item.completed","item":{"id":"item_1","type":"agent_message","text":"Hello from Codex"}}
            """;

        var parser = new CodexNdjsonParser();
        AgentTextDeltaEvent delta =
            Assert.IsType<AgentTextDeltaEvent>(Assert.Single(parser.ParseLine(line)));
        Assert.Equal("Hello from Codex", delta.Text);
    }

    [Fact]
    public void ParseLine_AgentMessageUpdated_StreamsIncrementalText()
    {
        var parser = new CodexNdjsonParser();

        const string first = """
            {"type":"item.updated","item":{"id":"item_1","type":"agent_message","text":"Hel"}}
            """;
        const string second = """
            {"type":"item.updated","item":{"id":"item_1","type":"agent_message","text":"Hello"}}
            """;

        AgentTextDeltaEvent firstDelta =
            Assert.IsType<AgentTextDeltaEvent>(Assert.Single(parser.ParseLine(first)));
        AgentTextDeltaEvent secondDelta =
            Assert.IsType<AgentTextDeltaEvent>(Assert.Single(parser.ParseLine(second)));

        Assert.Equal("Hel", firstDelta.Text);
        Assert.Equal("lo", secondDelta.Text);
    }

    [Fact]
    public void ParseLine_ReasoningStarted_ReturnsThinkingToolEvent()
    {
        const string line = """
            {"type":"item.started","item":{"id":"item_0","type":"reasoning","text":"Planning next steps"}}
            """;

        var parser = new CodexNdjsonParser();
        AgentToolEvent tool =
            Assert.IsType<AgentToolEvent>(Assert.Single(parser.ParseLine(line)));
        Assert.Equal("Thinking…", tool.Label);
    }

    [Fact]
    public void ParseLine_CommandExecutionStarted_ReturnsToolEvent()
    {
        const string line = """
            {"type":"item.started","item":{"id":"item_2","type":"command_execution","command":"dotnet test","status":"in_progress"}}
            """;

        var parser = new CodexNdjsonParser();
        AgentToolEvent tool =
            Assert.IsType<AgentToolEvent>(Assert.Single(parser.ParseLine(line)));
        Assert.Equal("Running dotnet test", tool.Label);
    }

    [Fact]
    public void ParseLine_TurnCompleted_ReturnsCompletedEvent()
    {
        const string line = """
            {"type":"turn.completed","usage":{"input_tokens":10,"output_tokens":5}}
            """;

        var parser = new CodexNdjsonParser();
        Assert.IsType<AgentCompletedEvent>(Assert.Single(parser.ParseLine(line)));
    }

    [Fact]
    public void ParseLine_TurnFailed_ReturnsErrorEvent()
    {
        const string line = """
            {"type":"turn.failed","error":{"message":"Something went wrong"}}
            """;

        var parser = new CodexNdjsonParser();
        AgentErrorEvent error =
            Assert.IsType<AgentErrorEvent>(Assert.Single(parser.ParseLine(line)));
        Assert.Equal("Agent.TurnFailed", error.Code);
        Assert.Equal("Something went wrong", error.Message);
    }

    [Fact]
    public void BuildArguments_IncludesModelAndContextAndResume()
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            AgentId = AgentIds.Codex,
            WorkspacePath = @"C:\repo",
            ModelId = "gpt-5.4",
            ContextSizeTokens = 272000,
            ExternalSessionId = "thread-abc"
        };

        IReadOnlyList<string> args = CreateArgumentBuilder().BuildArguments(
            session,
            "do the thing",
            [],
            entryScript: null);

        Assert.Equal(
            [
                "exec",
                "--json",
                "--skip-git-repo-check",
                "--sandbox",
                "workspace-write",
                "--model",
                "gpt-5.4",
                "-c",
                "approval_policy=\"on-request\"",
                "-c",
                "model_context_window=272000",
                "resume",
                "thread-abc",
                "do the thing"
            ],
            args);
    }

    [Fact]
    public void BuildArguments_EmitsHydratedCliConfigOverrides()
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            AgentId = AgentIds.Codex,
            WorkspacePath = @"C:\repo",
            ModelId = "gpt-5.4"
        };
        session.CliConfigOverrides["model_context_window"] = "272000";
        session.CliConfigOverrides["model_reasoning_effort"] = "high";
        session.CliConfigOverrides["approval_policy"] = "on-request";

        IReadOnlyList<string> args = CreateArgumentBuilder().BuildArguments(
            session,
            "do the thing",
            [],
            entryScript: null);

        Assert.Equal(
            [
                "exec",
                "--json",
                "--skip-git-repo-check",
                "--sandbox",
                "workspace-write",
                "--model",
                "gpt-5.4",
                "-c",
                "model_context_window=272000",
                "-c",
                "model_reasoning_effort=\"high\"",
                "-c",
                "approval_policy=\"on-request\"",
                "do the thing"
            ],
            args);
    }
}
