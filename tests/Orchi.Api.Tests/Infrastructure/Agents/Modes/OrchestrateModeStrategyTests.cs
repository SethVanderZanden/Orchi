using Orchi.Api.Infrastructure.Agents.Modes.Plan;
using Orchi.Api.Infrastructure.Agents.Modes.Strategies;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class OrchestrateModeStrategyTests
{
    [Fact]
    public void TryParseSubPlans_ParsesFencedJson()
    {
        const string text = """
            Here is the breakdown:
            ```json
            {"subPlans":[{"title":"API","contentMarkdown":"Build endpoints"},{"title":"UI","contentMarkdown":"Add panel"}]}
            ```
            """;

        bool parsed = OrchestrateModeStrategy.TryParseSubPlans(text, out IReadOnlyList<SubPlanInput> subPlans);

        Assert.True(parsed);
        Assert.Equal(2, subPlans.Count);
        Assert.Equal("API", subPlans[0].Title);
        Assert.Equal("Build endpoints", subPlans[0].ContentMarkdown);
    }

    [Fact]
    public void TryParseSubPlans_ReturnsFalse_WhenMissingSubPlans()
    {
        bool parsed = OrchestrateModeStrategy.TryParseSubPlans("no structured output", out _);

        Assert.False(parsed);
    }
}
