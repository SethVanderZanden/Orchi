using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Cursor;

namespace Orchi.Api.Tests.Infrastructure.Agents.Cursor;

public class CursorNdjsonParserTests
{
    [Fact]
    public void ParseLine_AssistantWithTimestamp_ReturnsTextDelta()
    {
        const string line = """
            {"type":"assistant","timestamp_ms":123,"message":{"content":[{"text":"Hello"}]}}
            """;

        AgentEvent[] events = CursorNdjsonParser.ParseLine(line).ToArray();

        AgentTextDeltaEvent delta = Assert.IsType<AgentTextDeltaEvent>(Assert.Single(events));
        Assert.Equal("Hello", delta.Text);
    }

    [Fact]
    public void ParseLine_AssistantWithModelCallId_IsIgnored()
    {
        const string line = """
            {"type":"assistant","model_call_id":"abc","message":{"content":[{"text":"Skip me"}]}}
            """;

        Assert.Empty(CursorNdjsonParser.ParseLine(line));
    }

    [Fact]
    public void ParseLine_SystemInit_ReturnsSessionStartedEvent()
    {
        const string line = """
            {"type":"system","subtype":"init","cwd":"/tmp","session_id":"cursor-session-init"}
            """;

        AgentSessionStartedEvent started = Assert.IsType<AgentSessionStartedEvent>(Assert.Single(CursorNdjsonParser.ParseLine(line)));
        Assert.Equal("cursor-session-init", started.ExternalSessionId);
    }

    [Fact]
    public void ParseLine_Result_ReturnsCompletedWithSessionId()
    {
        const string line = """
            {"type":"result","session_id":"cursor-session-1","result":"Final answer"}
            """;

        AgentCompletedEvent completed = Assert.IsType<AgentCompletedEvent>(Assert.Single(CursorNdjsonParser.ParseLine(line)));
        Assert.Equal("cursor-session-1", completed.ExternalSessionId);
        Assert.Equal("Final answer", completed.FullText);
    }

    [Fact]
    public void ParseLine_ToolStarted_ReturnsToolEvent()
    {
        const string line = """
            {"type":"tool_call","subtype":"started","tool_call":{"writeToolCall":{"args":{"path":"README.md"}}}}
            """;

        AgentToolEvent tool = Assert.IsType<AgentToolEvent>(Assert.Single(CursorNdjsonParser.ParseLine(line)));
        Assert.Equal("Writing README.md", tool.Label);
    }

    [Fact]
    public void ParseLine_ToolStartedWithArrayArgs_DoesNotThrowAndJoinsDetail()
    {
        const string line = """
            {"type":"tool_call","subtype":"started","tool_call":{"mcpToolCall":{"args":["-y","some-package"]}}}
            """;

        AgentEvent[] events = CursorNdjsonParser.ParseLine(line).ToArray();

        AgentToolEvent tool = Assert.IsType<AgentToolEvent>(Assert.Single(events));
        Assert.Equal("mcp -y some-package", tool.Label);
    }

    [Fact]
    public void ParseLine_ToolStartedWithShellCommand_ReturnsCommandDetail()
    {
        const string line = """
            {"type":"tool_call","subtype":"started","tool_call":{"shellToolCall":{"args":{"command":"ls -la"}}}}
            """;

        AgentToolEvent tool = Assert.IsType<AgentToolEvent>(Assert.Single(CursorNdjsonParser.ParseLine(line)));
        Assert.Equal("Running ls -la", tool.Label);
    }

    [Fact]
    public void ParseLine_ToolCompletedWithResultCommand_ReturnsCommandDetail()
    {
        const string line = """
            {"type":"tool_call","subtype":"completed","tool_call":{"shellToolCall":{"result":{"success":{"command":"ls -la"}}}}}
            """;

        AgentToolEvent tool = Assert.IsType<AgentToolEvent>(Assert.Single(CursorNdjsonParser.ParseLine(line)));
        Assert.Equal("Running ls -la (completed)", tool.Label);
    }

    [Fact]
    public void ParseLine_ToolStartedWithArrayArgs_DoesNotThrow()
    {
        const string line = """
            {"type":"tool_call","subtype":"started","tool_call":{"mcpToolCall":{"args":["-y","mcp-remote","https://example.com"]}}}
            """;

        Exception? exception = Record.Exception(() => CursorNdjsonParser.ParseLine(line).ToArray());

        Assert.Null(exception);
    }

    [Fact]
    public void ParseLine_AssistantWithoutTimestamp_IsIgnored()
    {
        const string line = """
            {"type":"assistant","message":{"content":[{"type":"text","text":"Full segment"}]}}
            """;

        Assert.Empty(CursorNdjsonParser.ParseLine(line));
    }

    [Fact]
    public void ParseLine_ToolFunction_UsesInnerNameAndArguments()
    {
        const string line = """
            {"type":"tool_call","subtype":"started","tool_call":{"function":{"name":"search","arguments":"{\"query\":\"readme\"}"}}}
            """;

        AgentToolEvent tool = Assert.IsType<AgentToolEvent>(Assert.Single(CursorNdjsonParser.ParseLine(line)));
        Assert.StartsWith("search ", tool.Label);
        Assert.Contains("query", tool.Label, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseLine_ToolStartedWithQuery_ReturnsQueryDetail()
    {
        const string line = """
            {"type":"tool_call","subtype":"started","tool_call":{"grepToolCall":{"args":{"query":"Program.cs"}}}}
            """;

        AgentToolEvent tool = Assert.IsType<AgentToolEvent>(Assert.Single(CursorNdjsonParser.ParseLine(line)));
        Assert.Equal("Searching Program.cs", tool.Label);
    }
}
