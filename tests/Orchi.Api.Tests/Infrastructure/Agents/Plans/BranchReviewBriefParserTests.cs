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

    [Theory]
    [InlineData("staging")]
    [InlineData("dev")]
    [InlineData("a")]
    [InlineData("feature-auth")]
    [InlineData("feature/very-long-branch-name-that-exceeds-limits")]
    public void ToBranchReviewWorktreeId_HandlesShortAndLongReviewIds(string headBranch)
    {
        string reviewId = ReviewBriefBuilder.ToBranchReviewId(headBranch);
        Guid suffix = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");

        string worktreeId = ReviewBriefBuilder.ToBranchReviewWorktreeId(reviewId, suffix);

        Assert.InRange(worktreeId.Length, 1, ReviewBriefBuilder.MaxWorktreeIdLength);
        Assert.StartsWith(reviewId, worktreeId);
    }

    [Fact]
    public void ToBranchReviewWorktreeId_ShortBranch_DoesNotThrowAndKeepsFullId()
    {
        // "branch-staging" (14) + "-" + 32-char N-guid = 47, previously crashed on [..48]
        string reviewId = ReviewBriefBuilder.ToBranchReviewId("staging");
        Guid suffix = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        string worktreeId = ReviewBriefBuilder.ToBranchReviewWorktreeId(reviewId, suffix);

        Assert.Equal("branch-staging-aaaaaaaabbbbccccddddeeeeeeeeeeee", worktreeId);
        Assert.Equal(47, worktreeId.Length);
    }

    [Fact]
    public void ToBranchReviewWorktreeId_LongBranch_TruncatesToMax()
    {
        string reviewId = ReviewBriefBuilder.ToBranchReviewId("feature/Auth_Flow_With_Extra_Segments");
        Guid suffix = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");

        string worktreeId = ReviewBriefBuilder.ToBranchReviewWorktreeId(reviewId, suffix);

        Assert.Equal(ReviewBriefBuilder.MaxWorktreeIdLength, worktreeId.Length);
        Assert.Equal($"{reviewId}-{suffix:N}"[..ReviewBriefBuilder.MaxWorktreeIdLength], worktreeId);
    }
}
