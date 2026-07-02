using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Cursor;

namespace Orchi.Api.Tests.Infrastructure.Agents.Cursor;

public class CursorAgentExecutableResolverTests
{
    [Fact]
    public void Resolve_AbsolutePath_WhenFileExists_ReturnsPath()
    {
        string tempDirectory = CreateTempDirectory();
        string executablePath = Path.Combine(tempDirectory, "agent.exe");
        File.WriteAllText(executablePath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            ExistingFiles = { executablePath }
        };

        var options = new CursorAgentOptions { Executable = executablePath };

        CursorAgentExecutableResolver.ResolveResult result =
            CursorAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.Equal(Path.GetFullPath(executablePath), result.ExecutablePath);
    }

    [Fact]
    public void Resolve_PathDirectory_FindsAgentExe()
    {
        string tempDirectory = CreateTempDirectory();
        string executablePath = Path.Combine(tempDirectory, "agent.exe");
        File.WriteAllText(executablePath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            PathDirectories = { tempDirectory },
            ExistingFiles = { executablePath }
        };

        var options = new CursorAgentOptions { Executable = "agent" };

        CursorAgentExecutableResolver.ResolveResult result =
            CursorAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.Equal(executablePath, result.ExecutablePath);
    }

    [Fact]
    public void Resolve_PrefersExeOverCmd()
    {
        string tempDirectory = CreateTempDirectory();
        string exePath = Path.Combine(tempDirectory, "agent.exe");
        string cmdPath = Path.Combine(tempDirectory, "agent.cmd");
        File.WriteAllText(exePath, string.Empty);
        File.WriteAllText(cmdPath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            PathDirectories = { tempDirectory },
            ExistingFiles = { exePath, cmdPath }
        };

        var options = new CursorAgentOptions { Executable = "agent" };

        CursorAgentExecutableResolver.ResolveResult result =
            CursorAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.Equal(exePath, result.ExecutablePath);
    }

    [Fact]
    public void Resolve_WindowsFallback_UsesLocalAppDataCursorAgentDirectory()
    {
        string tempDirectory = CreateTempDirectory();
        string installDirectory = Path.Combine(tempDirectory, "cursor-agent");
        Directory.CreateDirectory(installDirectory);
        string executablePath = Path.Combine(installDirectory, "agent.exe");
        File.WriteAllText(executablePath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            IsWindows = true,
            EnvironmentVariables =
            {
                ["LOCALAPPDATA"] = tempDirectory
            },
            ExistingFiles = { executablePath }
        };

        var options = new CursorAgentOptions { Executable = "agent" };

        CursorAgentExecutableResolver.ResolveResult result =
            CursorAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.Equal(executablePath, result.ExecutablePath);
    }

    [Fact]
    public void Resolve_AdditionalSearchPaths_AreCheckedBeforePath()
    {
        string tempDirectory = CreateTempDirectory();
        string executablePath = Path.Combine(tempDirectory, "custom-agent.exe");
        File.WriteAllText(executablePath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            ExistingFiles = { executablePath }
        };

        var options = new CursorAgentOptions
        {
            Executable = "custom-agent",
            AdditionalSearchPaths = [tempDirectory]
        };

        CursorAgentExecutableResolver.ResolveResult result =
            CursorAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.Equal(executablePath, result.ExecutablePath);
    }

    [Fact]
    public void Resolve_WhenNotFound_ReturnsActionableError()
    {
        var environment = new FakeExecutableEnvironment();
        var options = new CursorAgentOptions { Executable = "agent" };

        CursorAgentExecutableResolver.ResolveResult result =
            CursorAgentExecutableResolver.Resolve(options, environment);

        Assert.False(result.Success);
        Assert.Null(result.ExecutablePath);
        Assert.Contains("Unable to locate Cursor CLI executable", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("restart the Orchi API", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildArguments_DeduplicatesDefaultArgs()
    {
        var options = new CursorAgentOptions
        {
            DefaultArgs = ["--force", "--trust", "--force", "--trust"]
        };

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            AgentId = "cursor",
            WorkspacePath = @"E:\Projects\Orchi"
        };

        IReadOnlyList<string> arguments = CursorAgentAdapter.BuildArguments(options, session, "hello");

        Assert.Equal(["--force", "--trust", "-p", "--output-format", "stream-json", "--stream-partial-output", "--workspace", session.WorkspacePath, "hello"], arguments);
    }

    [Fact]
    public void BuildArguments_IncludesExtraCliArgsBeforeResume()
    {
        var options = new CursorAgentOptions { DefaultArgs = ["--force", "--trust"] };
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            AgentId = "cursor",
            WorkspacePath = @"E:\Projects\Orchi",
            ExternalSessionId = "resume-123"
        };

        IReadOnlyList<string> arguments = CursorAgentAdapter.BuildArguments(
            options,
            session,
            "hello",
            ["--mode=plan"]);

        Assert.Equal(
            [
                "--force", "--trust", "-p", "--output-format", "stream-json", "--stream-partial-output",
                "--workspace", session.WorkspacePath, "--mode=plan", "--resume", "resume-123", "hello"
            ],
            arguments);
    }

    private static string CreateTempDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }

    private sealed class FakeExecutableEnvironment : IExecutableEnvironment
    {
        public bool IsWindows { get; init; }

        public HashSet<string> ExistingFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> ExistingDirectories { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<string> PathDirectories { get; } = [];

        public Dictionary<string, string> EnvironmentVariables { get; } = new(StringComparer.OrdinalIgnoreCase);

        public string? GetEnvironmentVariable(string name) =>
            EnvironmentVariables.TryGetValue(name, out string? value) ? value : null;

        public string ExpandEnvironmentVariables(string value)
        {
            string expanded = value;

            foreach ((string key, string envValue) in EnvironmentVariables)
            {
                expanded = expanded.Replace($"%{key}%", envValue, StringComparison.OrdinalIgnoreCase);
            }

            return expanded;
        }

        public bool FileExists(string path) => ExistingFiles.Contains(path);

        public bool DirectoryExists(string path) =>
            ExistingDirectories.Contains(path) || Directory.Exists(path);

        public IReadOnlyList<string> GetPathDirectories() => PathDirectories;

        public IReadOnlyList<string> GetPathExtensions() => [".COM", ".EXE", ".BAT", ".CMD"];
    }
}
