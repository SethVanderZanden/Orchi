using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Options;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal sealed class CodexAgentAdapter(
    IOptions<CodexAgentOptions> options,
    ILogger<CodexAgentAdapter> logger) : IAgentAdapter
{
    public string AgentId => AgentIds.Codex;

    public async IAsyncEnumerable<AgentEvent> SendMessageAsync(
        ChatSession session,
        string prompt,
        IReadOnlyList<string> extraCliArgs,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        CodexAgentOptions config = options.Value;
        CodexAgentExecutableResolver.ResolveResult resolveResult = CodexAgentExecutableResolver.Resolve(config);

        if (!resolveResult.Success || resolveResult.Launch is null)
        {
            logger.LogError(
                "Unable to resolve Codex agent executable for chat {ChatId}: {Message}",
                session.Id,
                resolveResult.ErrorMessage);

            yield return new AgentErrorEvent("Agent.StartFailed", resolveResult.ErrorMessage!);
            yield break;
        }

        CodexAgentLaunchSpec launch = resolveResult.Launch;
        ProcessStartInfo startInfo = BuildStartInfo(launch, config, session, prompt, extraCliArgs);

        bool hasResume = !string.IsNullOrWhiteSpace(session.ExternalSessionId);
        logger.LogDebug(
            "Starting Codex agent for chat {ChatId}: launch={LaunchKind}, resume={HasResume}, externalSessionId={ExternalSessionId}",
            session.Id,
            launch.LaunchKind,
            hasResume,
            hasResume ? TruncateForLog(session.ExternalSessionId!) : "(none)");

        ProcessStartResult start = TryStartProcess(startInfo, session.Id, launch.ExecutablePath);
        if (start.Error is not null)
        {
            yield return start.Error;
            yield break;
        }

        Process process = start.Process!;

        lock (session.Sync)
        {
            session.RunningProcess = process;
        }

        using var registration = cancellationToken.Register(() => TryKillProcessTree(process, session.Id));

        try
        {
            await foreach (AgentEvent agentEvent in ReadEventsAsync(process, config.TimeoutSeconds, cancellationToken))
            {
                yield return agentEvent;

                if (agentEvent is AgentErrorEvent)
                {
                    yield break;
                }
            }

            AgentErrorEvent? exitCodeError = await TryGetExitCodeErrorAsync(process, cancellationToken);
            if (exitCodeError is not null)
            {
                yield return exitCodeError;
            }
        }
        finally
        {
            ReleaseRunningProcess(session, process);
        }
    }

    internal static IReadOnlyList<string> BuildArguments(
        CodexAgentOptions config,
        ChatSession session,
        string prompt,
        IReadOnlyList<string>? extraCliArgs = null,
        string? entryScript = null)
    {
        var arguments = new List<string>();

        if (!string.IsNullOrWhiteSpace(entryScript))
        {
            arguments.Add(entryScript);
        }

        arguments.Add("exec");
        arguments.Add("--json");

        foreach (string defaultArg in config.DefaultArgs.Distinct(StringComparer.Ordinal))
        {
            arguments.Add(defaultArg);
        }

        if (!string.IsNullOrWhiteSpace(session.ModelId))
        {
            arguments.Add("--model");
            arguments.Add(session.ModelId);
        }

        AgentCliConfigArgs.AppendOverrides(arguments, session.CliConfigOverrides);

        // Fallback for callers that set ContextSizeTokens without hydrating CliConfigOverrides.
        if (session.ContextSizeTokens is int tokens and > 0
            && !session.CliConfigOverrides.ContainsKey("model_context_window"))
        {
            AgentCliConfigArgs.AppendOverride(arguments, "model_context_window", tokens.ToString());
        }

        if (extraCliArgs is not null)
        {
            foreach (string extraArg in extraCliArgs)
            {
                if (!string.IsNullOrWhiteSpace(extraArg))
                {
                    arguments.Add(extraArg);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(session.ExternalSessionId))
        {
            arguments.Add("resume");
            arguments.Add(session.ExternalSessionId);
        }

        arguments.Add(prompt);
        return arguments;
    }

    private static ProcessStartInfo BuildStartInfo(
        CodexAgentLaunchSpec launch,
        CodexAgentOptions config,
        ChatSession session,
        string prompt,
        IReadOnlyList<string> extraCliArgs)
    {
        IReadOnlyList<string> arguments = BuildArguments(
            config,
            session,
            prompt,
            extraCliArgs,
            launch.EntryScript);

        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = session.WorkspacePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (launch.UsesCmdShim)
        {
            startInfo.FileName = OperatingSystem.IsWindows() ? "cmd.exe" : launch.ExecutablePath;
            if (OperatingSystem.IsWindows())
            {
                startInfo.ArgumentList.Add("/c");
                startInfo.ArgumentList.Add(launch.ExecutablePath);
            }
        }
        else
        {
            startInfo.FileName = launch.ExecutablePath;
        }

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private ProcessStartResult TryStartProcess(ProcessStartInfo startInfo, Guid chatId, string executablePath)
    {
        try
        {
            Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start Codex executable '{executablePath}'.");

            return new ProcessStartResult(process, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to start Codex agent for chat {ChatId}", chatId);
            return new ProcessStartResult(
                null,
                new AgentErrorEvent(
                    "Agent.StartFailed",
                    $"Unable to start Codex CLI ('{executablePath}'). Ensure Codex is installed and accessible."));
        }
    }

    private void TryKillProcessTree(Process process, Guid chatId)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to cancel Codex agent process for chat {ChatId}", chatId);
        }
    }

    private static void ReleaseRunningProcess(ChatSession session, Process process)
    {
        lock (session.Sync)
        {
            if (ReferenceEquals(session.RunningProcess, process))
            {
                session.RunningProcess = null;
            }
        }

        process.Dispose();
    }

    private static async Task<AgentErrorEvent?> TryGetExitCodeErrorAsync(
        Process process,
        CancellationToken cancellationToken)
    {
        if (!process.HasExited)
        {
            await process.WaitForExitAsync(cancellationToken);
        }

        if (process.ExitCode == 0 || cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        return new AgentErrorEvent(
            "Agent.ExitCode",
            $"Codex agent exited with code {process.ExitCode}.");
    }

    private async IAsyncEnumerable<AgentEvent> ReadEventsAsync(
        Process process,
        int timeoutSeconds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        Task<string> stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

        while (true)
        {
            (string? line, AgentErrorEvent? timeoutError) =
                await TryReadStdoutLineAsync(process, timeoutCts.Token, cancellationToken);

            if (timeoutError is not null)
            {
                yield return timeoutError;
                yield break;
            }

            if (line is null)
            {
                break;
            }

            foreach (AgentEvent agentEvent in CodexNdjsonParser.ParseLine(line))
            {
                yield return agentEvent;
            }
        }

        string stderr = await stderrTask;
        if (!string.IsNullOrWhiteSpace(stderr) && process.ExitCode != 0)
        {
            logger.LogWarning("Codex agent stderr for chat process: {StdErr}", stderr);
        }
    }

    private static async Task<(string? Line, AgentErrorEvent? TimeoutError)> TryReadStdoutLineAsync(
        Process process,
        CancellationToken timeoutToken,
        CancellationToken userToken)
    {
        try
        {
            string? line = await process.StandardOutput.ReadLineAsync(timeoutToken);
            return (line, null);
        }
        catch (OperationCanceledException) when (!userToken.IsCancellationRequested)
        {
            return (null, new AgentErrorEvent("Agent.Timeout", "Codex agent timed out."));
        }
    }

    private sealed record ProcessStartResult(Process? Process, AgentErrorEvent? Error);

    private static string TruncateForLog(string value, int maxLength = 12) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
