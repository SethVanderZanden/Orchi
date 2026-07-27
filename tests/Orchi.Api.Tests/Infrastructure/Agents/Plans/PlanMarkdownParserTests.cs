using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Plans;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

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

    [Fact]
    public void ExtractPlans_TreatsSingleLinePathAsFileReference()
    {
        const string content = """
            <!-- orchi-plan:auth-refactor -->
            .orchi/plan-auth-refactor.md
            <!-- /orchi-plan -->
            """;

        IReadOnlyList<PlanMarkdownParser.ParsedPlan> plans = PlanMarkdownParser.ExtractPlans(content);

        Assert.Single(plans);
        Assert.Equal("auth-refactor", plans[0].PlanId);
        Assert.Equal(".orchi/plan-auth-refactor.md", plans[0].PlanFilePath);
        Assert.Empty(plans[0].ContentMarkdown);
    }

    [Fact]
    public async Task HydratePlansFromWorkspaceAsync_LoadsReferencedPlanFiles()
    {
        string workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-plan-hydrate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(workspacePath, ".orchi"));

        try
        {
            const string planContent = """
                # Auth refactor

                Implement JWT.
                """;

            await File.WriteAllTextAsync(
                Path.Combine(workspacePath, ".orchi", "plan-auth-refactor.md"),
                planContent);

            var messages = new List<ChatMessage>
            {
                new(
                    Guid.NewGuid(),
                    "assistant",
                    """
                    <!-- orchi-plan:auth-refactor -->
                    .orchi/plan-auth-refactor.md
                    <!-- /orchi-plan -->
                    """,
                    DateTimeOffset.UtcNow,
                    "complete")
            };

            IReadOnlyList<PlanMarkdownParser.ParsedPlan> plans =
                await PlanMarkdownParser.HydratePlansFromWorkspaceAsync(
                    workspacePath,
                    messages,
                    new OrchiArtifactFileStore(),
                    CancellationToken.None);

            Assert.Single(plans);
            Assert.Equal("Auth refactor", plans[0].Title);
            Assert.Contains("Implement JWT", plans[0].ContentMarkdown);
        }
        finally
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }
}
