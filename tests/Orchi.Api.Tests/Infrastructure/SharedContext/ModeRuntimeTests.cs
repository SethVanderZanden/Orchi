using Orchi.SharedContext.Modes;

namespace Orchi.Api.Tests.Infrastructure.SharedContext;

public class ModeRuntimeTests
{
    private readonly IModeRuntime _runtime = new ModeRuntime();

    [Theory]
    [InlineData("plan", "orchestrate", true)]
    [InlineData("agent", "implement", true)]
    [InlineData("participant", "plan", false)]
    [InlineData("plan", "participant", false)]
    public void ShouldPreserveResume_RespectsCliProfileCompatibility(
        string from,
        string to,
        bool expected)
    {
        bool result = _runtime.ShouldPreserveResume(from, to);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveCliProfile_MapsParticipantToAsk()
    {
        CursorCliProfile profile = _runtime.ResolveCliProfile("participant");
        Assert.Equal(CursorCliProfileKind.Ask, profile.Kind);
        Assert.Contains("--mode=ask", profile.ExtraArgs);
    }
}
