using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Template method: shared one-turn CLI lifecycle (resolve → spawn → stream stdout → exit).
/// Agent-specific behavior is supplied via <see cref="IAgentCliProcessorProfile"/> strategies.
/// </summary>
internal sealed class AgentCliTurnProcessor(ILogger<AgentCliTurnProcessor> logger)
{
    public async IAsyncEnumerable<AgentEvent> RunTurnAsync(
        IAgentCliProcessorProfile profile,
        ChatSession session,
        string prompt,
        IReadOnlyList<string> extraCliArgs,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        AgentLaunchResolveResult resolveResult =
            await profile.LaunchResolver.ResolveAsync(cancellationToken);

        if (!resolveResult.Success || resolveResult.Launch is null)
        {
            logger.LogError(
                "Unable to resolve {Agent} executable for chat {ChatId}: {Message}",
                profile.DisplayName,
                session.Id,
                resolveResult.ErrorMessage);

            yield return new AgentErrorEvent("Agent.StartFailed", resolveResult.ErrorMessage!);
            yield break;
        }

        AgentLaunchSpec launch = resolveResult.Launch;
        IReadOnlyList<string> arguments = profile.ArgumentBuilder.BuildArguments(
            session,
            prompt,
            extraCliArgs,
            launch.EntryScript);

        ProcessStartInfo startInfo = AgentProcessStartInfoBuilder.Build(
            launch,
            session.WorkspacePath,
            arguments,
            profile.UseWindowsCmdShim);

        bool hasResume = !string.IsNullOrWhiteSpace(session.ExternalSessionId);
        logger.LogDebug(
            "Starting {Agent} for chat {ChatId}: launch={LaunchKind}, resume={HasResume}, externalSessionId={ExternalSessionId}",
            profile.DisplayName,
            session.Id,
            launch.LaunchKind,
            hasResume,
            hasResume ? TruncateForLog(session.ExternalSessionId!) : "(none)");

        ProcessStartResult start = TryStartProcess(
            startInfo,
            session.Id,
            launch.ExecutablePath,
            profile.DisplayName);
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

        using var registration = cancellationToken.Register(() => TryKillProcessTree(process, session.Id, profile.DisplayName));

        try
        {
            await foreach (AgentEvent agentEvent in ReadEventsAsync(
                               profile,
                               process,
                               cancellationToken))
            {
                yield return agentEvent;

                if (agentEvent is AgentErrorEvent)
                {
                    yield break;
                }
            }

            AgentErrorEvent? exitCodeError = await TryGetExitCodeErrorAsync(process, profile.DisplayName, cancellationToken);
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

    private ProcessStartResult TryStartProcess(
        ProcessStartInfo startInfo,
        Guid chatId,
        string executablePath,
        string displayName)
    {
        try
        {
            Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start {displayName} executable '{executablePath}'.");

            // Codex (and similar CLIs) block when stdin is a pipe until EOF. Headless API hosts
            // often inherit an open stdin pipe, which leaves the agent idle with no JSON output.
            try
            {
                process.StandardInput.Close();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to close stdin for {Agent} chat {ChatId}", displayName, chatId);
            }

            return new ProcessStartResult(process, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to start {Agent} for chat {ChatId}", displayName, chatId);
            return new ProcessStartResult(
                null,
                new AgentErrorEvent(
                    "Agent.StartFailed",
                    $"Unable to start {displayName} CLI ('{executablePath}'). Ensure it is installed and accessible."));
        }
    }

    private void TryKillProcessTree(Process process, Guid chatId, string displayName)
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
            logger.LogDebug(ex, "Failed to cancel {Agent} process for chat {ChatId}", displayName, chatId);
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
        string displayName,
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
            $"{displayName} agent exited with code {process.ExitCode}.");
    }

    private async IAsyncEnumerable<AgentEvent> ReadEventsAsync(
        IAgentCliProcessorProfile profile,
        Process process,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(profile.TimeoutSeconds));

        Task<string> stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
        IAgentStreamLineParser parser = profile.CreateLineParser();
        int parsedEventCount = 0;

        while (true)
        {
            (string? line, AgentErrorEvent? timeoutError) =
                await TryReadStdoutLineAsync(process, profile.DisplayName, timeoutCts.Token, cancellationToken);

            if (timeoutError is not null)
            {
                yield return timeoutError;
                yield break;
            }

            if (line is null)
            {
                break;
            }

            foreach (AgentEvent agentEvent in parser.ParseLine(line))
            {
                parsedEventCount++;
                yield return agentEvent;
            }
        }

        string stderr = await stderrTask;
        if (profile.SurfaceStderrWhenNoParsedEvents
            && parsedEventCount == 0
            && !string.IsNullOrWhiteSpace(stderr))
        {
            logger.LogWarning(
                "{Agent} produced no JSON events; stderr: {StdErr}",
                profile.DisplayName,
                TruncateForLog(stderr.Trim(), 500));

            yield return new AgentErrorEvent(
                "Agent.NoEvents",
                $"{profile.DisplayName} produced no output. {TruncateForLog(stderr.Trim(), 300)}");
        }
        else if (!string.IsNullOrWhiteSpace(stderr) && process.ExitCode != 0)
        {
            logger.LogWarning("{Agent} stderr for chat process: {StdErr}", profile.DisplayName, stderr);
        }
    }

    private static async Task<(string? Line, AgentErrorEvent? TimeoutError)> TryReadStdoutLineAsync(
        Process process,
        string displayName,
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
            return (null, new AgentErrorEvent("Agent.Timeout", $"{displayName} agent timed out."));
        }
    }

    private sealed record ProcessStartResult(Process? Process, AgentErrorEvent? Error);

    private static string TruncateForLog(string value, int maxLength = 12) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
