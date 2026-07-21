using System.Diagnostics;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Codex;
using Orchi.Api.Infrastructure.Agents.Cursor;

namespace Orchi.Api.Tests.Infrastructure.Agents.Codex;

public class CodexAgentExecutableResolverTests
{
    [Fact]
    public void Resolve_AbsolutePath_WhenFileExists_ReturnsPath()
    {
        string tempDirectory = CreateTempDirectory();
        string executablePath = Path.Combine(tempDirectory, "codex.exe");
        File.WriteAllText(executablePath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            ExistingFiles = { executablePath }
        };

        var options = new CodexAgentOptions { Executable = executablePath };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(Path.GetFullPath(executablePath), result.Launch.ExecutablePath);
        Assert.Null(result.Launch.EntryScript);
    }

    [Fact]
    public void Resolve_PathDirectory_FindsCodexCmd()
    {
        string tempDirectory = CreateTempDirectory();
        string cmdPath = Path.Combine(tempDirectory, "codex.cmd");
        File.WriteAllText(cmdPath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            PathDirectories = { tempDirectory },
            ExistingFiles = { cmdPath }
        };

        var options = new CodexAgentOptions { Executable = "codex" };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(cmdPath, result.Launch.ExecutablePath);
        Assert.True(result.Launch.UsesCmdShim);
    }

    [Fact]
    public void Resolve_Windows_SkipsExtensionlessBashShimWhenCmdExists()
    {
        string tempDirectory = CreateTempDirectory();
        string shimPath = Path.Combine(tempDirectory, "codex");
        string cmdPath = Path.Combine(tempDirectory, "codex.cmd");
        File.WriteAllText(shimPath, "#!/usr/bin/env node");
        File.WriteAllText(cmdPath, "@echo off");

        var environment = new FakeExecutableEnvironment
        {
            IsWindows = true,
            PathDirectories = { tempDirectory },
            ExistingFiles = { shimPath, cmdPath }
        };

        var options = new CodexAgentOptions { Executable = "codex" };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(cmdPath, result.Launch.ExecutablePath);
    }

    [Fact]
    public void Resolve_PrefersExeOverCmd()
    {
        string tempDirectory = CreateTempDirectory();
        string exePath = Path.Combine(tempDirectory, "codex.exe");
        string cmdPath = Path.Combine(tempDirectory, "codex.cmd");
        File.WriteAllText(exePath, string.Empty);
        File.WriteAllText(cmdPath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            PathDirectories = { tempDirectory },
            ExistingFiles = { exePath, cmdPath }
        };

        var options = new CodexAgentOptions { Executable = "codex" };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(exePath, result.Launch.ExecutablePath);
    }

    [Fact]
    public void Resolve_PrefersNpmNodeBundleOverCmdShim()
    {
        string tempDirectory = CreateTempDirectory();
        string nodePath = Path.Combine(tempDirectory, "node.exe");
        string codexJsPath = Path.Combine(
            tempDirectory,
            "node_modules",
            "@openai",
            "codex",
            "bin",
            "codex.js");
        string cmdPath = Path.Combine(tempDirectory, "codex.cmd");
        Directory.CreateDirectory(Path.GetDirectoryName(codexJsPath)!);
        File.WriteAllText(nodePath, string.Empty);
        File.WriteAllText(codexJsPath, string.Empty);
        File.WriteAllText(cmdPath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            IsWindows = true,
            PathDirectories = { tempDirectory },
            ExistingFiles = { nodePath, codexJsPath, cmdPath }
        };

        var options = new CodexAgentOptions { Executable = "codex" };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(nodePath, result.Launch.ExecutablePath);
        Assert.Equal(codexJsPath, result.Launch.EntryScript);
        Assert.Equal("node-bundle", result.Launch.LaunchKind);
    }

    [Fact]
    public void Resolve_WindowsFallback_UsesAppDataNpmDirectory()
    {
        string tempDirectory = CreateTempDirectory();
        string npmDirectory = Path.Combine(tempDirectory, "npm");
        Directory.CreateDirectory(npmDirectory);
        string cmdPath = Path.Combine(npmDirectory, "codex.cmd");
        File.WriteAllText(cmdPath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            IsWindows = true,
            EnvironmentVariables =
            {
                ["APPDATA"] = tempDirectory
            },
            ExistingFiles = { cmdPath }
        };

        var options = new CodexAgentOptions { Executable = "codex" };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(cmdPath, result.Launch.ExecutablePath);
    }

    [Fact]
    public void Resolve_WhenNotFound_ReturnsActionableError()
    {
        var environment = new FakeExecutableEnvironment();
        var options = new CodexAgentOptions { Executable = "codex" };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.False(result.Success);
        Assert.Null(result.Launch);
        Assert.Contains("Unable to locate Codex CLI executable", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("restart the Orchi API", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildArguments_IncludesEntryScriptBeforeExec()
    {
        var options = new CodexAgentOptions { DefaultArgs = ["--skip-git-repo-check"] };
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            AgentId = "codex",
            WorkspacePath = @"E:\Projects\Orchi"
        };

        string codexJsPath = @"C:\npm\node_modules\@openai\codex\bin\codex.js";
        IReadOnlyList<string> arguments = CodexAgentAdapter.BuildArguments(
            options,
            session,
            "hello",
            entryScript: codexJsPath);

        Assert.Equal(
            [
                codexJsPath, "exec", "--json", "--skip-git-repo-check", "--sandbox", "workspace-write", "--ask-for-approval", "never", "hello"
            ],
            arguments);
    }

    [Fact]
    public void BuildStartInfo_CmdShim_UsesCmdExeOnWindows()
    {
        var launch = new CodexAgentLaunchSpec(@"C:\Program Files\nodejs\codex.cmd", null);
        var options = new CodexAgentOptions();
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            AgentId = "codex",
            WorkspacePath = @"E:\Projects\Orchi"
        };

        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        ProcessStartInfo startInfo = InvokeBuildStartInfo(launch, options, session, "hello");

        Assert.Equal("cmd.exe", startInfo.FileName);
        Assert.Equal(["/c", launch.ExecutablePath, "exec", "--json", "--skip-git-repo-check", "--sandbox", "workspace-write", "--ask-for-approval", "never", "hello"], startInfo.ArgumentList);
    }

    private static ProcessStartInfo InvokeBuildStartInfo(
        CodexAgentLaunchSpec launch,
        CodexAgentOptions options,
        ChatSession session,
        string prompt)
    {
        var method = typeof(CodexAgentAdapter).GetMethod(
            "BuildStartInfo",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        return (ProcessStartInfo)method.Invoke(
            null,
            [launch, options, session, prompt, Array.Empty<string>()])!;
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

        public IReadOnlyList<string> GetDirectories(string path)
        {
            if (Directory.Exists(path))
            {
                return Directory.GetDirectories(path);
            }

            return [];
        }

        public IReadOnlyList<string> GetPathDirectories() => PathDirectories;

        public IReadOnlyList<string> GetPathExtensions() => [".COM", ".EXE", ".BAT", ".CMD"];
    }
}
