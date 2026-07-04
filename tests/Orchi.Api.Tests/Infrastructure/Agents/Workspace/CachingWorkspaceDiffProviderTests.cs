using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orchi.Api.Infrastructure.Agents.Workspace;
using Orchi.Api.Infrastructure.Caching;

namespace Orchi.Api.Tests.Infrastructure.Agents.Workspace;

public class CachingWorkspaceDiffProviderTests
{
    [Fact]
    public void GetDiff_SecondCallWithSameHead_DoesNotInvokeInnerAgain()
    {
        var inner = new CountingWorkspaceDiffProvider("cached diff");
        CachingWorkspaceDiffProvider provider = CreateProvider(inner);

        string first = provider.GetDiff(inner.WorkspacePath);
        string second = provider.GetDiff(inner.WorkspacePath);

        Assert.Equal("cached diff", first);
        Assert.Equal("cached diff", second);
        Assert.Equal(1, inner.CallCount);
    }

    private static CachingWorkspaceDiffProvider CreateProvider(CountingWorkspaceDiffProvider inner)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:DefaultExpirationMinutes"] = "5",
                ["Cache:WorkspaceDiffExpirationSeconds"] = "30",
                ["Cache:Distributed:Enabled"] = "false"
            })
            .Build();

        services.AddOrchiCaching(configuration);

        ServiceProvider providerServices = services.BuildServiceProvider();
        OrchiHybridCacheService cache = providerServices.GetRequiredService<OrchiHybridCacheService>();
        return new CachingWorkspaceDiffProvider(inner, cache);
    }

    private sealed class CountingWorkspaceDiffProvider(string diff) : IWorkspaceDiffProvider
    {
        public string WorkspacePath { get; } = InitializeGitWorkspace();

        public int CallCount { get; private set; }

        public string GetDiff(string workspacePath)
        {
            CallCount++;
            return diff;
        }

        private static string InitializeGitWorkspace()
        {
            if (!IsGitAvailable())
            {
                throw new InvalidOperationException("Git is required for CachingWorkspaceDiffProviderTests.");
            }

            string workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-cache-diff-{Guid.NewGuid():N}");
            Directory.CreateDirectory(workspacePath);

            RunGit(workspacePath, "init");
            File.WriteAllText(Path.Combine(workspacePath, "tracked.txt"), "initial\n");
            RunGit(workspacePath, "add", "tracked.txt");
            RunGit(
                workspacePath,
                "-c", "user.email=test@example.com",
                "-c", "user.name=Test",
                "commit", "-m", "init");

            return workspacePath;
        }

        private static void RunGit(string workspacePath, params string[] args)
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.WorkingDirectory = workspacePath;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            foreach (string arg in args)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            process.Start();
            process.WaitForExit();
        }

        private static bool IsGitAvailable()
        {
            try
            {
                using var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "git";
                process.StartInfo.ArgumentList.Add("--version");
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
