using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Tests.Infrastructure.Agents.Plans;

public class PlanMarkdownParserTests
{
    [Fact]
    public void TryExtractPlanContent_FindsMatchingPlan()
    {
        const string content = """
            <!-- orchi-plan:auth-refactor -->
            # Auth refactor

            Implement JWT.
            <!-- /orchi-plan -->
            """;

        string? plan = PlanMarkdownParser.TryExtractPlanContent(content, "auth-refactor");

        Assert.NotNull(plan);
        Assert.Contains("Implement JWT", plan);
    }

    [Fact]
    public void TryExtractPlanFromMessages_PrefersLatestAssistantMessage()
    {
        var messages = new List<ChatMessage>
        {
            new(Guid.NewGuid(), "assistant", "<!-- orchi-plan:auth-refactor -->\n# v1\n<!-- /orchi-plan -->", DateTimeOffset.UtcNow, "complete"),
            new(Guid.NewGuid(), "assistant", "<!-- orchi-plan:auth-refactor -->\n# v2\n<!-- /orchi-plan -->", DateTimeOffset.UtcNow, "complete")
        };

        string? plan = PlanMarkdownParser.TryExtractPlanFromMessages(messages, "auth-refactor");

        Assert.NotNull(plan);
        Assert.Contains("# v2", plan);
    }

    [Fact]
    public void TryExtractPlanIdFromPath_ParsesPlanFilePaths()
    {
        Assert.Equal("auth-refactor", PlanMarkdownParser.TryExtractPlanIdFromPath(".orchi/plan-auth-refactor.md"));
        Assert.Null(PlanMarkdownParser.TryExtractPlanIdFromPath(".orchi/review-auth-refactor.md"));
    }
}
