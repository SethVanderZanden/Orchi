using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Orchi.Api.Infrastructure.Agents.Cli;
using Orchi.Api.Infrastructure.Caching;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal sealed class CursorAgentAdapter(
    IOptions<CursorAgentOptions> options,
    OrchiHybridCacheService cache,
    ILogger<CursorAgentAdapter> logger) : IAgentAdapter
{
    public string AgentId => AgentIds.Cursor;

    public async IAsyncEnumerable<AgentEvent> SendMessageAsync(
        ChatSession session,
        string prompt,
        IReadOnlyList<string> extraCliArgs,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        CursorAgentOptions config = options.Value;

        CursorAgentExecutableResolver.ResolveResult resolveResult =
            await ResolveExecutableAsync(config, cancellationToken);

        if (!resolveResult.Success || resolveResult.Launch is null)
        {
            logger.LogError(
                "Unable to resolve Cursor agent executable for chat {ChatId}: {Message}",
                session.Id,
                resolveResult.ErrorMessage);

            yield return new AgentErrorEvent("Agent.StartFailed", resolveResult.ErrorMessage!);
            yield break;
        }

        AgentCliLaunchSpec launch = resolveResult.Launch;
        ProcessStartInfo startInfo = BuildStartInfo(launch, config, session, prompt, extraCliArgs);

        bool hasResume = !string.IsNullOrWhiteSpace(session.ExternalSessionId);
        logger.LogDebug(
            "Starting Cursor agent for chat {ChatId}: launch={LaunchKind}, resume={HasResume}, externalSessionId={ExternalSessionId}",
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

    private async ValueTask<CursorAgentExecutableResolver.ResolveResult> ResolveExecutableAsync(
        CursorAgentOptions config,
        CancellationToken cancellationToken)
    {
        string cacheKey = OrchiCacheKeys.CursorExecutable(BuildExecutableConfigFingerprint(config));

        CachedCursorExecutableResolution cached = await cache.GetOrCreateAsync(
            cacheKey,
            _ => ValueTask.FromResult(
                CachedCursorExecutableResolution.From(CursorAgentExecutableResolver.Resolve(config))),
            cache.CreateCursorExecutableEntryOptions(),
            cancellationToken);

        return cached.ToResolveResult();
    }

    private static string BuildExecutableConfigFingerprint(CursorAgentOptions config)
    {
        var parts = new List<string> { config.Executable };

        if (config.AdditionalSearchPaths is { Length: > 0 })
        {
            parts.AddRange(config.AdditionalSearchPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Order(StringComparer.OrdinalIgnoreCase));
        }

        return string.Join('\u001f', parts);
    }

    internal static IReadOnlyList<string> BuildArguments(
        CursorAgentOptions config,
        ChatSession session,
        string prompt,
        IReadOnlyList<string>? extraCliArgs = null)
    {
        var arguments = new List<string>();

        foreach (string defaultArg in config.DefaultArgs.Distinct(StringComparer.Ordinal))
        {
            arguments.Add(defaultArg);
        }

        arguments.Add("-p");
        arguments.Add("--output-format");
        arguments.Add("stream-json");
        arguments.Add("--stream-partial-output");
        arguments.Add("--workspace");
        arguments.Add(session.WorkspacePath);

        if (!string.IsNullOrWhiteSpace(session.ModelId))
        {
            arguments.Add("--model");
            arguments.Add(session.ModelId);
        }

        if (extraCliArgs is not null)
        {
            AppendNonWhiteSpaceArgs(arguments, extraCliArgs);
        }

        AppendResumeArgs(arguments, session);
        arguments.Add(prompt);
        return arguments;
    }

    private static void AppendNonWhiteSpaceArgs(List<string> arguments, IEnumerable<string> extraCliArgs)
    {
        foreach (string extraArg in extraCliArgs)
        {
            if (string.IsNullOrWhiteSpace(extraArg))
            {
                continue;
            }

            arguments.Add(extraArg);
        }
    }

    private static void AppendResumeArgs(List<string> arguments, ChatSession session)
    {
        if (string.IsNullOrWhiteSpace(session.ExternalSessionId))
        {
            return;
        }

        arguments.Add("--resume");
        arguments.Add(session.ExternalSessionId);
    }

    private static ProcessStartInfo BuildStartInfo(
        AgentCliLaunchSpec launch,
        CursorAgentOptions config,
        ChatSession session,
        string prompt,
        IReadOnlyList<string> extraCliArgs)
    {
        IReadOnlyList<string> arguments = BuildArguments(
            config,
            session,
            prompt,
            extraCliArgs);

        return AgentCliProcessStart.Create(launch, session.WorkspacePath, arguments);
    }

    private ProcessStartResult TryStartProcess(ProcessStartInfo startInfo, Guid chatId, string executablePath)
    {
        try
        {
            Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start Cursor agent executable '{executablePath}'.");

            return new ProcessStartResult(process, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to start Cursor agent for chat {ChatId}", chatId);
            return new ProcessStartResult(
                null,
                new AgentErrorEvent(
                    "Agent.StartFailed",
                    $"Unable to start Cursor CLI ('{executablePath}'). Ensure the Cursor agent is installed and accessible."));
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
            logger.LogDebug(ex, "Failed to cancel Cursor agent process for chat {ChatId}", chatId);
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

    private static async Task<AgentErrorEvent?> TryGetExitCodeErrorAsync(Process process, CancellationToken cancellationToken)
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
            $"Cursor agent exited with code {process.ExitCode}.");
    }

    /// <summary>
    /// Reads NDJSON lines from process stdout and yields parsed <see cref="AgentEvent"/> values.
    /// Stderr is started via <see cref="StreamReader.ReadToEndAsync"/> before the stdout loop and
    /// awaited after — see docs/agents/concurrent-pipe-reading.md#dummy-section-start-here.
    /// </summary>
    private async IAsyncEnumerable<AgentEvent> ReadEventsAsync(
        Process process,
        int timeoutSeconds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        // Start draining stderr now; await after stdout loop (see doc link on this method).
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

            foreach (AgentEvent agentEvent in CursorNdjsonParser.ParseLine(line))
            {
                yield return agentEvent;
            }
        }

        string stderr = await stderrTask;
        if (!string.IsNullOrWhiteSpace(stderr) && process.ExitCode != 0)
        {
            logger.LogWarning("Cursor agent stderr for chat process: {StdErr}", stderr);
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
            return (null, new AgentErrorEvent("Agent.Timeout", "Cursor agent timed out."));
        }
    }

    private sealed record ProcessStartResult(Process? Process, AgentErrorEvent? Error);

    private static string TruncateForLog(string value, int maxLength = 12) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
