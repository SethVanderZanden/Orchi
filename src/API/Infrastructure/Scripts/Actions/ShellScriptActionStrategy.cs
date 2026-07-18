using Orchi.Api.Infrastructure.Cli;

namespace Orchi.Api.Infrastructure.Scripts.Actions;

public sealed class ShellScriptActionStrategy(IProcessRunner processRunner) : IScriptActionStrategy
{
    public string Kind => ScriptStepKinds.Shell;

    public async Task<ScriptActionResult> ExecuteAsync(
        ScriptActionContext context,
        CancellationToken cancellationToken)
    {
        string command = context.Step.Command?.Trim() ?? string.Empty;
        string label = $"Running {command}";

        if (string.IsNullOrWhiteSpace(command))
        {
            return new ScriptActionResult(false, label, Error: "shell step is missing a command.");
        }

        bool isWindows = OperatingSystem.IsWindows();
        string fileName = isWindows ? "cmd.exe" : "/bin/sh";
        string[] args = isWindows
            ? ["/c", command]
            : ["-c", command];

        ProcessRunResult result = await processRunner.RunAsync(
            fileName,
            args,
            context.WorkspacePath,
            cancellationToken,
            timeoutMs: 600_000);

        if (result.Succeeded)
        {
            return new ScriptActionResult(true, label, result.CombinedOutput);
        }

        return new ScriptActionResult(false, label, result.CombinedOutput, result.CombinedOutput);
    }
}
