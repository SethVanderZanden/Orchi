using System.Diagnostics;
using Microsoft.Extensions.Options;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Cli;
using Orchi.Api.Infrastructure.Agents.Codex;
using Orchi.Api.Tests.Infrastructure.Agents.Cli;

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
    public void Resolve_PrefersPathCmdOverNestedNpmVendorBinary()
    {
        // Regression: digging into @openai/codex-win32-*/vendor/.../codex.exe produced
        // MAX_PATH failures ("The filename or extension is too long") with deep worktrees.
        string tempDirectory = CreateTempDirectory();
        string cmdPath = Path.Combine(tempDirectory, "codex.cmd");
        string nestedVendorExe = Path.Combine(
            tempDirectory,
            "node_modules",
            "@openai",
            "codex",
            "node_modules",
            "@openai",
            "codex-win32-x64",
            "vendor",
            "x86_64-pc-windows-msvc",
            "bin",
            "codex.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(nestedVendorExe)!);
        File.WriteAllText(cmdPath, "@echo off");
        File.WriteAllText(nestedVendorExe, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            IsWindows = true,
            PathDirectories = { tempDirectory },
            ExistingFiles = { cmdPath, nestedVendorExe }
        };

        var options = new CodexAgentOptions { Executable = "codex" };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(cmdPath, result.Launch.ExecutablePath);
        Assert.True(result.Launch.UsesCmdShim);
        Assert.DoesNotContain("vendor", result.Launch.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("codex-win32", result.Launch.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_PrefersNativeExeOverNpmNodeBundle()
    {
        string tempDirectory = CreateTempDirectory();
        string nodePath = Path.Combine(tempDirectory, "node.exe");
        string codexExePath = Path.Combine(tempDirectory, "codex.exe");
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
        File.WriteAllText(codexExePath, string.Empty);
        File.WriteAllText(codexJsPath, string.Empty);
        File.WriteAllText(cmdPath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            IsWindows = true,
            PathDirectories = { tempDirectory },
            ExistingFiles = { nodePath, codexExePath, codexJsPath, cmdPath }
        };

        var options = new CodexAgentOptions { Executable = "codex" };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(codexExePath, result.Launch.ExecutablePath);
        Assert.Null(result.Launch.EntryScript);
        Assert.Equal("direct", result.Launch.LaunchKind);
    }

    [Fact]
    public void Resolve_WindowsStandaloneInstaller_UsesLocalAppDataProgramsPath()
    {
        string tempDirectory = CreateTempDirectory();
        string binDirectory = Path.Combine(tempDirectory, "Programs", "OpenAI", "Codex", "bin");
        Directory.CreateDirectory(binDirectory);
        string exePath = Path.Combine(binDirectory, "codex.exe");
        File.WriteAllText(exePath, string.Empty);

        // Stale npm node-bundle under Program Files should not win over the standalone installer.
        string nodejsDirectory = Path.Combine(tempDirectory, "nodejs");
        string nodePath = Path.Combine(nodejsDirectory, "node.exe");
        string codexJsPath = Path.Combine(
            nodejsDirectory,
            "node_modules",
            "@openai",
            "codex",
            "bin",
            "codex.js");
        Directory.CreateDirectory(Path.GetDirectoryName(codexJsPath)!);
        File.WriteAllText(nodePath, string.Empty);
        File.WriteAllText(codexJsPath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            IsWindows = true,
            EnvironmentVariables =
            {
                ["LOCALAPPDATA"] = tempDirectory,
                ["ProgramFiles"] = tempDirectory
            },
            ExistingFiles = { exePath, nodePath, codexJsPath },
            ExistingDirectories = { binDirectory, nodejsDirectory }
        };

        var options = new CodexAgentOptions { Executable = "codex" };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(exePath, result.Launch.ExecutablePath);
        Assert.Null(result.Launch.EntryScript);
        Assert.Equal("direct", result.Launch.LaunchKind);
    }

    [Fact]
    public void Resolve_DoesNotPreferNestedNpmPlatformBinaryOverPathCmd()
    {
        string tempDirectory = CreateTempDirectory();
        string cmdPath = Path.Combine(tempDirectory, "codex.cmd");
        string nativeExePath = Path.Combine(
            tempDirectory,
            "node_modules",
            "@openai",
            "codex",
            "node_modules",
            "@openai",
            "codex-win32-x64",
            "bin",
            "codex.exe");
        string codexJsPath = Path.Combine(
            tempDirectory,
            "node_modules",
            "@openai",
            "codex",
            "bin",
            "codex.js");
        Directory.CreateDirectory(Path.GetDirectoryName(nativeExePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(codexJsPath)!);
        File.WriteAllText(cmdPath, "@echo off");
        File.WriteAllText(nativeExePath, string.Empty);
        File.WriteAllText(codexJsPath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            IsWindows = true,
            PathDirectories = { tempDirectory },
            ExistingFiles = { cmdPath, nativeExePath, codexJsPath }
        };

        var options = new CodexAgentOptions { Executable = "codex" };

        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(options, environment);

        Assert.True(result.Success);
        Assert.NotNull(result.Launch);
        Assert.Equal(cmdPath, result.Launch.ExecutablePath);
        Assert.Null(result.Launch.EntryScript);
        Assert.True(result.Launch.UsesCmdShim);
    }

    [Fact]
    public void Resolve_UsesNpmNodeBundleWhenOnlyNodeBundleExists()
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
        Directory.CreateDirectory(Path.GetDirectoryName(codexJsPath)!);
        File.WriteAllText(nodePath, string.Empty);
        File.WriteAllText(codexJsPath, string.Empty);

        var environment = new FakeExecutableEnvironment
        {
            IsWindows = true,
            PathDirectories = { tempDirectory },
            ExistingFiles = { nodePath, codexJsPath }
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
    public void Resolve_CmdShim_PreferredOverNpmNodeBundle()
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
        Assert.Equal(cmdPath, result.Launch.ExecutablePath);
        Assert.Null(result.Launch.EntryScript);
        Assert.True(result.Launch.UsesCmdShim);
        Assert.Equal("cmd-shim", result.Launch.LaunchKind);
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
        Assert.Contains("OpenAI\\Codex\\bin\\codex.exe", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
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
        var builder = new CodexCliArgumentBuilder(Options.Create(options));
        IReadOnlyList<string> arguments = builder.BuildArguments(
            session,
            "hello",
            [],
            codexJsPath);

        Assert.Equal(
            [
                codexJsPath, "exec", "--json", "--skip-git-repo-check", "hello"
            ],
            arguments);
    }

    [Fact]
    public void BuildStartInfo_CmdShim_UsesCmdExeOnWindows()
    {
        var launch = new AgentLaunchSpec(@"C:\Program Files\nodejs\codex.cmd", null);
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

        var builder = new CodexCliArgumentBuilder(Options.Create(options));
        IReadOnlyList<string> arguments = builder.BuildArguments(session, "hello", [], null);
        ProcessStartInfo startInfo = AgentProcessStartInfoBuilder.Build(
            launch,
            session.WorkspacePath,
            arguments,
            useWindowsCmdShim: true);

        Assert.Equal("cmd.exe", startInfo.FileName);
        Assert.True(startInfo.RedirectStandardInput);
        Assert.Equal(["/c", launch.ExecutablePath, "exec", "--json", "--skip-git-repo-check", "--sandbox", "workspace-write", "hello"], startInfo.ArgumentList);
    }

    private static string CreateTempDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }
}
