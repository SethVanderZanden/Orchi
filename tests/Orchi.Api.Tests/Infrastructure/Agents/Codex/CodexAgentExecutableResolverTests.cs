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
            IsWindows = true,
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
    public void Resolve_PathDirectory_PrefersCmdOverExtensionlessShim()
    {
        string tempDirectory = CreateTempDirectory();
        string shimPath = Path.Combine(tempDirectory, "codex");
        string cmdPath = Path.Combine(tempDirectory, "codex.cmd");
        File.WriteAllText(shimPath, "#!/bin/sh");
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
        Assert.Equal("cmd-shim", result.Launch.LaunchKind);
    }

    [Fact]
    public void Resolve_PrefersNativeBinaryOverCmdShim()
    {
        string tempDirectory = CreateTempDirectory();
        string nativePath = Path.Combine(
            tempDirectory,
            "node_modules",
            "@openai",
            "codex",
            "node_modules",
            "@openai",
            "codex-win32-x64",
            "vendor",
            "x86_64-pc-windows-msvc",
            "codex",
            "codex.exe");
        string cmdPath = Path.Combine(tempDirectory, "codex.cmd");

        Directory.CreateDirectory(Path.GetDirectoryName(nativePath)!);
        File.WriteAllText(nativePath, string.Empty);
        File.WriteAllText(cmdPath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            IsWindows = true,
            PathDirectories = { tempDirectory },
            ExistingFiles = { nativePath, cmdPath }
        };

        var options = new CodexAgentOptions { Executable = "codex" };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(nativePath, result.Launch.ExecutablePath);
        Assert.Equal("direct", result.Launch.LaunchKind);
    }

    [Fact]
    public void Resolve_PrefersNodeBundleOverCmdShim()
    {
        string tempDirectory = CreateTempDirectory();
        string nodePath = Path.Combine(tempDirectory, "node.exe");
        string codexJs = Path.Combine(tempDirectory, "node_modules", "@openai", "codex", "bin", "codex.js");
        string cmdPath = Path.Combine(tempDirectory, "codex.cmd");

        Directory.CreateDirectory(Path.GetDirectoryName(codexJs)!);
        File.WriteAllText(nodePath, string.Empty);
        File.WriteAllText(codexJs, string.Empty);
        File.WriteAllText(cmdPath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            IsWindows = true,
            PathDirectories = { tempDirectory },
            ExistingFiles = { nodePath, codexJs, cmdPath }
        };

        var options = new CodexAgentOptions { Executable = "codex" };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(nodePath, result.Launch.ExecutablePath);
        Assert.Equal(codexJs, result.Launch.EntryScript);
        Assert.Equal("node-bundle", result.Launch.LaunchKind);
    }

    [Fact]
    public void Resolve_WindowsFallback_UsesProgramFilesNodeJsDirectory()
    {
        string tempDirectory = CreateTempDirectory();
        string installDirectory = Path.Combine(tempDirectory, "nodejs");
        Directory.CreateDirectory(installDirectory);
        string executablePath = Path.Combine(installDirectory, "codex.cmd");
        File.WriteAllText(executablePath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            IsWindows = true,
            EnvironmentVariables =
            {
                ["ProgramFiles"] = tempDirectory
            },
            ExistingFiles = { executablePath }
        };

        var options = new CodexAgentOptions { Executable = "codex" };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(executablePath, result.Launch.ExecutablePath);
    }

    [Fact]
    public void Resolve_AdditionalSearchPaths_AreCheckedBeforePath()
    {
        string tempDirectory = CreateTempDirectory();
        string executablePath = Path.Combine(tempDirectory, "codex.exe");
        File.WriteAllText(executablePath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            IsWindows = true,
            ExistingFiles = { executablePath }
        };

        var options = new CodexAgentOptions
        {
            Executable = "codex",
            AdditionalSearchPaths = [tempDirectory]
        };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(executablePath, result.Launch.ExecutablePath);
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
