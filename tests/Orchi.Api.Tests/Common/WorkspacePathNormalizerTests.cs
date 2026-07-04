using Orchi.Api.Common;

namespace Orchi.Api.Tests.Common;

public class WorkspacePathNormalizerTests
{
    [Theory]
    [InlineData(@"E:\Projects\Orchi\", @"e:\projects\orchi")]
    [InlineData(@"E:/Projects/Orchi", @"e:\projects\orchi")]
    [InlineData("/home/user/project", "/home/user/project")]
    [InlineData(@"/home/user/project\", "/home/user/project")]
    public void Normalize_MatchesDesktopRules(string input, string expected)
    {
        Assert.Equal(expected, WorkspacePathNormalizer.Normalize(input));
    }

    [Fact]
    public void DeriveNameFromPath_ReturnsLastSegment()
    {
        Assert.Equal("Orchi", WorkspacePathNormalizer.DeriveNameFromPath(@"E:\Projects\Orchi"));
        Assert.Equal("project", WorkspacePathNormalizer.DeriveNameFromPath("/home/user/project"));
    }
}
