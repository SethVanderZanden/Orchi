using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class ConversationContextBuilderTests
{
    private readonly ConversationContextBuilder _builder = new();

    [Fact]
    public void BuildDynamicSuffix_WhenResumeMissing_IncludesHistoryBlock()
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            AgentId = "cursor",
            WorkspacePath = Directory.GetCurrentDirectory(),
            Mode = ChatMode.Agent
        };
        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "user", "context", DateTimeOffset.UtcNow));
        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "assistant", "answer", DateTimeOffset.UtcNow, Status: "complete"));
        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "user", "next", DateTimeOffset.UtcNow));

        string dynamic = _builder.BuildDynamicSuffix(session, "next");

        Assert.Contains("## Conversation so far", dynamic, StringComparison.Ordinal);
        Assert.Contains("context", dynamic, StringComparison.Ordinal);
        Assert.Contains("next", dynamic, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDynamicSuffix_WithMiddleSection_PlacesHistoryBeforeMiddleAndUserMessage()
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            AgentId = "cursor",
            WorkspacePath = Directory.GetCurrentDirectory(),
            Mode = ChatMode.Implement,
            ExternalSessionId = "cursor-session-1"
        };
        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "user", "prior", DateTimeOffset.UtcNow));
        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "assistant", "done", DateTimeOffset.UtcNow, Status: "complete"));
        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "user", "implement this", DateTimeOffset.UtcNow));

        string dynamic = _builder.BuildDynamicSuffix(session, "implement this", "## Attached plan\n\nstep 1");

        int historyIndex = dynamic.IndexOf("## Conversation so far", StringComparison.Ordinal);
        int planIndex = dynamic.IndexOf("## Attached plan", StringComparison.Ordinal);
        int userIndex = dynamic.IndexOf("implement this", StringComparison.Ordinal);

        Assert.True(historyIndex >= 0);
        Assert.True(planIndex > historyIndex);
        Assert.True(userIndex > planIndex);
    }
}
