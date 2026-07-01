using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class ConversationHistoryFormatterTests
{
    [Fact]
    public void Format_WithNoPriorMessages_ReturnsNull()
    {
        var session = CreateSession();

        string? history = ConversationHistoryFormatter.Format(session, "hello");

        Assert.Null(history);
    }

    [Fact]
    public void Format_ExcludesCurrentUserMessageAndIncludesPriorTurns()
    {
        var session = CreateSession();
        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "user", "first question", DateTimeOffset.UtcNow));
        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "assistant", "first answer", DateTimeOffset.UtcNow, Status: "complete"));
        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "user", "follow up", DateTimeOffset.UtcNow));

        string? history = ConversationHistoryFormatter.Format(session, "follow up");

        Assert.NotNull(history);
        Assert.Contains("first question", history, StringComparison.Ordinal);
        Assert.Contains("first answer", history, StringComparison.Ordinal);
        Assert.DoesNotContain("follow up", history, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_WhenResumeActive_CapsToSafetyNetMessageCount()
    {
        var session = CreateSession();
        session.ExternalSessionId = "cursor-session-1";

        for (int i = 0; i < 12; i++)
        {
            session.Messages.Add(new ChatMessage(Guid.NewGuid(), "user", $"user-{i}", DateTimeOffset.UtcNow));
            session.Messages.Add(new ChatMessage(
                Guid.NewGuid(),
                "assistant",
                $"assistant-{i}",
                DateTimeOffset.UtcNow,
                Status: "complete"));
        }

        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "user", "latest", DateTimeOffset.UtcNow));

        string? history = ConversationHistoryFormatter.Format(session, "latest");

        Assert.NotNull(history);
        Assert.DoesNotContain("user-0", history, StringComparison.Ordinal);
        Assert.DoesNotContain("assistant-0", history, StringComparison.Ordinal);
        Assert.Contains("user-11", history, StringComparison.Ordinal);
        Assert.Contains("assistant-11", history, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_WhenResumeMissing_IncludesAllPriorMessages()
    {
        var session = CreateSession();
        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "user", "older", DateTimeOffset.UtcNow));
        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "assistant", "reply", DateTimeOffset.UtcNow, Status: "complete"));
        session.Messages.Add(new ChatMessage(Guid.NewGuid(), "user", "newer", DateTimeOffset.UtcNow));

        string? history = ConversationHistoryFormatter.Format(session, "newer");

        Assert.NotNull(history);
        Assert.Contains("older", history, StringComparison.Ordinal);
        Assert.Contains("reply", history, StringComparison.Ordinal);
    }

    private static ChatSession CreateSession() =>
        new()
        {
            Id = Guid.NewGuid(),
            AgentId = "cursor",
            WorkspacePath = Directory.GetCurrentDirectory(),
            Mode = ChatMode.Agent
        };
}
