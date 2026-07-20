using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Tests.Infrastructure.Agents.Cli;

public class AgentCliProcessStartTests
{
    [Fact]
    public void Create_DirectExecutable_UsesFileNameAndArgumentList()
    {
        var launch = new AgentCliLaunchSpec(@"C:\Tools\codex.exe", null);

        var startInfo = AgentCliProcessStart.Create(
            launch,
            @"C:\workspace",
            ["exec", "--json", "hello world"]);

        Assert.Equal(@"C:\Tools\codex.exe", startInfo.FileName);
        Assert.Equal(@"C:\workspace", startInfo.WorkingDirectory);
        Assert.False(startInfo.UseShellExecute);
        Assert.Equal(["exec", "--json", "hello world"], startInfo.ArgumentList.ToArray());
    }

    [Fact]
    public void Create_NodeBundle_PrependsEntryScript()
    {
        var launch = new AgentCliLaunchSpec(@"C:\nodejs\node.exe", @"C:\nodejs\node_modules\@openai\codex\bin\codex.js");

        var startInfo = AgentCliProcessStart.Create(
            launch,
            workingDirectory: null,
            ["exec", "--json"]);

        Assert.Equal(@"C:\nodejs\node.exe", startInfo.FileName);
        Assert.Equal(
            [@"C:\nodejs\node_modules\@openai\codex\bin\codex.js", "exec", "--json"],
            startInfo.ArgumentList.ToArray());
    }

    [Fact]
    public void BuildWindowsShellCommand_QuotesScriptAndArguments()
    {
        string command = AgentCliProcessStart.BuildWindowsShellCommand(
            @"C:\Program Files\nodejs\codex.cmd",
            ["exec", "hello & world"]);

        Assert.Equal(
            "\"C:\\Program Files\\nodejs\\codex.cmd\" exec \"hello & world\"",
            command);
    }
}
