using Orchi.Api.Infrastructure.Agents.Cli;
using Orchi.Api.Infrastructure.Agents.Codex;

namespace Orchi.Api.Tests.Infrastructure.Agents.Cli;

public class AgentCliLoginPathEnricherTests
{
    [Fact]
    public void ParsePathDirectories_SplitsAndDedupes()
    {
        IReadOnlyList<string> dirs = AgentCliLoginPathEnricher.ParsePathDirectories(
            "/opt/homebrew/bin:/usr/local/bin:/opt/homebrew/bin:",
            ':');

        Assert.Equal(["/opt/homebrew/bin", "/usr/local/bin"], dirs);
    }

    [Fact]
    public void ParsePathDirectories_Empty_ReturnsEmpty()
    {
        Assert.Empty(AgentCliLoginPathEnricher.ParsePathDirectories(null, ':'));
        Assert.Empty(AgentCliLoginPathEnricher.ParsePathDirectories("   ", ':'));
    }
}

public class AgentCliCommandResolverResultTests
{
    [Fact]
    public void Resolve_StampsPlatformInstallAndLaunchKind()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        string cmdPath = Path.Combine(tempDirectory, "codex.cmd");
        File.WriteAllText(cmdPath, "@echo off");

        var environment = new FakeExecutableEnvironment
        {
            HostPlatform = AgentCliHostPlatform.Windows,
            HostArchitecture = AgentCliHostArchitecture.X64,
            PathDirectories = { tempDirectory },
            ExistingFiles = { cmdPath },
            EnvironmentVariables =
            {
                ["APPDATA"] = Path.Combine(tempDirectory, "AppData", "Roaming")
            }
        };

        CodexAgentExecutableResolver.ResolveResult result = CodexAgentExecutableResolver.Resolve(
            new CodexAgentOptions { Executable = "codex" },
            environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(cmdPath, result.Launch.ExecutablePath);
        Assert.Equal(AgentCliHostPlatform.Windows, result.HostPlatform);
        Assert.Equal(AgentCliInstallKind.Unknown, result.InstallKind);
        Assert.Equal("cmd-shim", result.LaunchKind);
    }

    [Fact]
    public void Resolve_NpmStylePath_ClassifiesInstallKind()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string npmDirectory = Path.Combine(tempDirectory, "npm");
        Directory.CreateDirectory(npmDirectory);
        string cmdPath = Path.Combine(npmDirectory, "codex.cmd");
        File.WriteAllText(cmdPath, "@echo off");

        var environment = new FakeExecutableEnvironment
        {
            HostPlatform = AgentCliHostPlatform.Windows,
            PathDirectories = { npmDirectory },
            ExistingFiles = { cmdPath }
        };

        CodexAgentExecutableResolver.ResolveResult result = CodexAgentExecutableResolver.Resolve(
            new CodexAgentOptions { Executable = "codex" },
            environment);

        Assert.True(result.Success);
        Assert.Equal(AgentCliInstallKind.NpmGlobal, result.InstallKind);
    }

    private sealed class FakeExecutableEnvironment : IExecutableEnvironment
    {
        public AgentCliHostPlatform HostPlatform { get; init; } = AgentCliHostPlatform.Linux;

        public AgentCliHostArchitecture HostArchitecture { get; init; } = AgentCliHostArchitecture.X64;

        public HashSet<string> ExistingFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<string> PathDirectories { get; } = [];

        public Dictionary<string, string> EnvironmentVariables { get; } = new(StringComparer.OrdinalIgnoreCase);

        public string? GetEnvironmentVariable(string name) =>
            EnvironmentVariables.TryGetValue(name, out string? value) ? value : null;

        public string ExpandEnvironmentVariables(string value) => value;

        public bool FileExists(string path) => ExistingFiles.Contains(path);

        public bool DirectoryExists(string path) =>
            PathDirectories.Contains(path) || Directory.Exists(path);

        public IReadOnlyList<string> GetDirectories(string path) =>
            Directory.Exists(path) ? Directory.GetDirectories(path) : [];

        public IReadOnlyList<string> GetPathDirectories() => PathDirectories;

        public IReadOnlyList<string> GetPathExtensions() => [".COM", ".EXE", ".BAT", ".CMD"];
    }
}
