using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Cli;
using Orchi.Api.Infrastructure.Git.Hosting;

namespace Orchi.Api.Tests.Infrastructure.Git;

public class GitHostAdapterFactoryTests
{
    [Fact]
    public void GetAdapter_ResolvesGitHubAndAzureDevOps()
    {
        var runner = new ProcessRunner();
        var factory = new GitHostAdapterFactory(
        [
            new GitHubHostAdapter(runner),
            new AzureDevOpsHostAdapter(runner)
        ]);

        IGitHostAdapter github = factory.GetAdapter(GitHostProvider.GitHub);
        IGitHostAdapter azure = factory.GetAdapter(GitHostProvider.AzureDevOps);

        Assert.Equal(GitHostProviderIds.GitHub, github.ProviderId);
        Assert.Equal(GitHostProviderIds.AzureDevOps, azure.ProviderId);
        Assert.Equal("gh", github.RequiredCli);
        Assert.Equal("az", azure.RequiredCli);
    }

    [Fact]
    public async Task Facade_CreatePullRequest_FailsWhenNotReady()
    {
        var facade = new GitHostingFacade(
            new GitHostAdapterFactory(
            [
                new GitHubHostAdapter(new ProcessRunner()),
                new AzureDevOpsHostAdapter(new ProcessRunner())
            ]));

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            facade.CreatePullRequestAsync(
                GitHostProvider.GitHub,
                new CreatePullRequestRequest(
                    Directory.GetCurrentDirectory(),
                    "title",
                    "body",
                    "feature",
                    "main"),
                CancellationToken.None));

        Assert.Contains("not ready", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
