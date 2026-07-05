using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Tests.Infrastructure.Agents.Plans;

public class PlanSequenceMarkdownParserTests
{
    [Fact]
    public void TryParseSequence_ParsesPlanIdsInOrder()
    {
        const string content = """
            <!-- orchi-plan-sequence -->
            first-plan
            second-plan
            - third-plan
            <!-- /orchi-plan-sequence -->
            """;

        IReadOnlyList<string>? sequence = PlanSequenceMarkdownParser.TryParseSequence(content);

        Assert.NotNull(sequence);
        Assert.Equal(["first-plan", "second-plan", "third-plan"], sequence);
    }

    [Fact]
    public void ParseSequenceFromMessages_UsesLatestAssistantBlock()
    {
        var messages = new List<ChatMessage>
        {
            new(Guid.NewGuid(), "assistant", """
                <!-- orchi-plan-sequence -->
                old-plan
                <!-- /orchi-plan-sequence -->
                """, DateTimeOffset.UtcNow, "complete"),
            new(Guid.NewGuid(), "assistant", """
                <!-- orchi-plan-sequence -->
                new-plan
                <!-- /orchi-plan-sequence -->
                """, DateTimeOffset.UtcNow, "complete")
        };

        IReadOnlyList<string> sequence = PlanSequenceMarkdownParser.ParseSequenceFromMessages(messages);

        Assert.Equal(["new-plan"], sequence);
    }

    [Fact]
    public void ExtractAllPlansFromMessages_MergesPlansAcrossMessages()
    {
        var messages = new List<ChatMessage>
        {
            new(Guid.NewGuid(), "assistant", """
                <!-- orchi-plan:alpha -->
                # Alpha
                Alpha body
                <!-- /orchi-plan -->
                """, DateTimeOffset.UtcNow, "complete"),
            new(Guid.NewGuid(), "assistant", """
                <!-- orchi-plan:beta -->
                # Beta
                Beta body
                <!-- /orchi-plan -->
                """, DateTimeOffset.UtcNow, "complete")
        };

        IReadOnlyList<PlanMarkdownParser.ParsedPlan> plans =
            PlanMarkdownParser.ExtractAllPlansFromMessages(messages);

        Assert.Equal(2, plans.Count);
        Assert.Contains(plans, plan => plan.PlanId == "alpha" && plan.Title == "Alpha");
        Assert.Contains(plans, plan => plan.PlanId == "beta" && plan.Title == "Beta");
    }
}
