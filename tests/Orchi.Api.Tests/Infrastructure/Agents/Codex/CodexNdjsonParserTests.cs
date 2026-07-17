using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Codex;

namespace Orchi.Api.Tests.Infrastructure.Agents.Codex;

public class CodexNdjsonParserTests
{
    [Fact]
    public void ParseLine_ThreadStarted_ReturnsSessionStartedEvent()
    {
        const string line = """
            {"type":"thread.started","thread_id":"codex-thread-1"}
            """;

        AgentSessionStartedEvent started =
            Assert.IsType<AgentSessionStartedEvent>(Assert.Single(CodexNdjsonParser.ParseLine(line)));
        Assert.Equal("codex-thread-1", started.ExternalSessionId);
    }

    [Fact]
    public void ParseLine_AgentMessageCompleted_ReturnsTextDelta()
    {
        const string line = """
            {"type":"item.completed","item":{"id":"item_1","type":"agent_message","text":"Hello from Codex"}}
            """;

        AgentTextDeltaEvent delta =
            Assert.IsType<AgentTextDeltaEvent>(Assert.Single(CodexNdjsonParser.ParseLine(line)));
        Assert.Equal("Hello from Codex", delta.Text);
    }

    [Fact]
    public void ParseLine_CommandExecutionStarted_ReturnsToolEvent()
    {
        const string line = """
            {"type":"item.started","item":{"id":"item_2","type":"command_execution","command":"dotnet test","status":"in_progress"}}
            """;

        AgentToolEvent tool =
            Assert.IsType<AgentToolEvent>(Assert.Single(CodexNdjsonParser.ParseLine(line)));
        Assert.Equal("Running dotnet test", tool.Label);
    }

    [Fact]
    public void ParseLine_TurnCompleted_ReturnsCompletedEvent()
    {
        const string line = """
            {"type":"turn.completed","usage":{"input_tokens":10,"output_tokens":5}}
            """;

        Assert.IsType<AgentCompletedEvent>(Assert.Single(CodexNdjsonParser.ParseLine(line)));
    }

    [Fact]
    public void ParseLine_TurnFailed_ReturnsErrorEvent()
    {
        const string line = """
            {"type":"turn.failed","message":"Something went wrong"}
            """;

        AgentErrorEvent error =
            Assert.IsType<AgentErrorEvent>(Assert.Single(CodexNdjsonParser.ParseLine(line)));
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

        IReadOnlyList<string> args = CodexAgentAdapter.BuildArguments(
            new CodexAgentOptions(),
            session,
            "do the thing");

        Assert.Equal(
            [
                "exec",
                "--json",
                "--skip-git-repo-check",
                "--model",
                "gpt-5.4",
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

        IReadOnlyList<string> args = CodexAgentAdapter.BuildArguments(
            new CodexAgentOptions(),
            session,
            "do the thing");

        Assert.Equal(
            [
                "exec",
                "--json",
                "--skip-git-repo-check",
                "--model",
                "gpt-5.4",
                "-c",
                "approval_policy=\"on-request\"",
                "-c",
                "model_context_window=272000",
                "-c",
                "model_reasoning_effort=\"high\"",
                "do the thing"
            ],
            args);
    }
}
