using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Tests.Infrastructure.Agents.Plans;

public class BranchReviewBriefParserTests
{
    [Fact]
    public void TryParse_ReadsHeadAndBaseFromMarker()
    {
        string brief = ReviewBriefBuilder.BuildForBranchReview(
            "branch-feature-auth",
            "feature/auth",
            "main");

        BranchReviewBriefParser.BranchReviewRefs? parsed = BranchReviewBriefParser.TryParse(brief);

        Assert.NotNull(parsed);
        Assert.Equal("feature/auth", parsed.HeadBranch);
        Assert.Equal("main", parsed.BaseBranch);
    }

    [Fact]
    public void ToBranchReviewId_SanitizesBranchName()
    {
        string id = ReviewBriefBuilder.ToBranchReviewId("feature/Auth_Flow");

        Assert.Equal("branch-feature-auth-flow", id);
    }

    [Fact]
    public void Build_WithBranchPair_IncludesBranchReviewMarker()
    {
        string brief = ReviewBriefBuilder.Build(
            "auth-refactor",
            "# Plan",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "orchi/20260724-abc123",
            "main");

        BranchReviewBriefParser.BranchReviewRefs? parsed = BranchReviewBriefParser.TryParse(brief);

        Assert.NotNull(parsed);
        Assert.Equal("orchi/20260724-abc123", parsed.HeadBranch);
        Assert.Equal("main", parsed.BaseBranch);
        Assert.Contains("main...orchi/20260724-abc123", brief);
    }
}
