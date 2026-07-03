using Microsoft.Extensions.Logging.Abstractions;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt.Behaviours;
using Orchi.Api.Tests.Infrastructure.Agents.Modes;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes.Prompt;

public class PromptComposerBehaviourTests
{
    [Fact]
    public void LoggingPromptComposer_DelegatesToInnerComposer()
    {
        IAgentPromptComposer inner = PromptTestHelpers.CreateComposer();
        var composer = new LoggingPromptComposer(inner, NullLogger<LoggingPromptComposer>.Instance);
        var session = PromptTestHelpers.CreateSession();

        string prompt = composer.Compose(session, "hello");

        Assert.Contains("<message>hello</message>", prompt);
        Assert.Empty(composer.GetExtraCliArgs(DefaultAgentModeStrategy.Mode));
    }
}
