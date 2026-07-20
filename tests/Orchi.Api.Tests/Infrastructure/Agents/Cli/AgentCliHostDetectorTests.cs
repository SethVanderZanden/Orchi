using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Tests.Infrastructure.Agents.Cli;

public class AgentCliHostDetectorTests
{
    [Theory]
    [InlineData(@"C:\Users\dev\AppData\Roaming\npm\codex.cmd", AgentCliHostPlatform.Windows, AgentCliInstallKind.NpmGlobal)]
    [InlineData("/opt/homebrew/bin/codex", AgentCliHostPlatform.MacOS, AgentCliInstallKind.Homebrew)]
    [InlineData("/home/linuxbrew/.linuxbrew/bin/codex", AgentCliHostPlatform.Linux, AgentCliInstallKind.Homebrew)]
    [InlineData("/Users/dev/.volta/bin/codex", AgentCliHostPlatform.MacOS, AgentCliInstallKind.Volta)]
    [InlineData(@"C:\Users\dev\AppData\Local\cursor-agent\agent.exe", AgentCliHostPlatform.Windows, AgentCliInstallKind.NativeInstaller)]
    [InlineData("/usr/local/bin/codex", AgentCliHostPlatform.MacOS, AgentCliInstallKind.Unknown)]
    public void DetectInstallKind_ClassifiesFromPath(
        string path,
        AgentCliHostPlatform platform,
        AgentCliInstallKind expected)
    {
        Assert.Equal(expected, AgentCliHostDetector.DetectInstallKind(path, platform));
    }
}

public class AgentCliKnownDirectoriesTests
{
    [Fact]
    public void For_MacOS_IncludesHomebrewAndNpmGlobal()
    {
        var environment = new FakeExecutableEnvironment
        {
            HostPlatform = AgentCliHostPlatform.MacOS,
            EnvironmentVariables = { ["HOME"] = "/Users/dev" }
        };

        List<string> dirs = AgentCliKnownDirectories.For(environment).ToList();

        Assert.Contains("/opt/homebrew/bin", dirs);
        Assert.Contains("/usr/local/bin", dirs);
        Assert.Contains("/Users/dev/.npm-global/bin", dirs);
        Assert.Contains("/Users/dev/.volta/bin", dirs);
    }

    [Fact]
    public void For_Linux_IncludesLocalBinAndLinuxbrew()
    {
        var environment = new FakeExecutableEnvironment
        {
            HostPlatform = AgentCliHostPlatform.Linux,
            EnvironmentVariables = { ["HOME"] = "/home/dev" }
        };

        List<string> dirs = AgentCliKnownDirectories.For(environment).ToList();

        Assert.Contains("/usr/local/bin", dirs);
        Assert.Contains("/home/dev/.local/bin", dirs);
        Assert.Contains("/home/linuxbrew/.linuxbrew/bin", dirs);
    }

    private sealed class FakeExecutableEnvironment : IExecutableEnvironment
    {
        public AgentCliHostPlatform HostPlatform { get; init; }

        public AgentCliHostArchitecture HostArchitecture { get; init; } = AgentCliHostArchitecture.X64;

        public Dictionary<string, string> EnvironmentVariables { get; } = new(StringComparer.OrdinalIgnoreCase);

        public string? GetEnvironmentVariable(string name) =>
            EnvironmentVariables.TryGetValue(name, out string? value) ? value : null;

        public string ExpandEnvironmentVariables(string value) => value;

        public bool FileExists(string path) => false;

        public bool DirectoryExists(string path) => false;

        public IReadOnlyList<string> GetDirectories(string path) => [];

        public IReadOnlyList<string> GetPathDirectories() => [];

        public IReadOnlyList<string> GetPathExtensions() => [];
    }
}
