using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Options;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal sealed class CursorAgentAdapter(
    IOptions<CursorAgentOptions> options,
    ILogger<CursorAgentAdapter> logger) : IAgentAdapter
{
    public string AgentId => "cursor";

    public async IAsyncEnumerable<AgentEvent> SendMessageAsync(
        ChatSession session,
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        CursorAgentOptions config = options.Value;

        CursorAgentExecutableResolver.ResolveResult resolveResult =
            CursorAgentExecutableResolver.Resolve(config);

        if (!resolveResult.Success)
        {
            logger.LogError(
                "Unable to resolve Cursor agent executable for chat {ChatId}: {Message}",
                session.Id,
                resolveResult.ErrorMessage);

            yield return new AgentErrorEvent("Agent.StartFailed", resolveResult.ErrorMessage!);
            yield break;
        }

        ProcessStartInfo startInfo = BuildStartInfo(resolveResult.ExecutablePath!, config, session, prompt);

        Process process;
        AgentErrorEvent? startError = null;
        try
        {
            process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start Cursor agent executable '{resolveResult.ExecutablePath}'.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to start Cursor agent for chat {ChatId}", session.Id);
            startError = new AgentErrorEvent(
                "Agent.StartFailed",
                $"Unable to start Cursor CLI ('{resolveResult.ExecutablePath}'). Ensure the Cursor agent is installed and accessible.");
            process = null!;
        }

        if (startError is not null)
        {
            yield return startError;
            yield break;
        }

        lock (session.Sync)
        {
            session.RunningProcess = process;
        }

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to cancel Cursor agent process for chat {ChatId}", session.Id);
            }
        });

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

            if (!process.HasExited)
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            if (process.ExitCode != 0 && !cancellationToken.IsCancellationRequested)
            {
                yield return new AgentErrorEvent(
                    "Agent.ExitCode",
                    $"Cursor agent exited with code {process.ExitCode}.");
            }
        }
        finally
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
    }

    internal static IReadOnlyList<string> BuildArguments(CursorAgentOptions config, ChatSession session, string prompt)
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

        if (!string.IsNullOrWhiteSpace(session.ExternalSessionId))
        {
            arguments.Add("--resume");
            arguments.Add(session.ExternalSessionId);
        }

        arguments.Add(prompt);
        return arguments;
    }

    private static ProcessStartInfo BuildStartInfo(
        string executablePath,
        CursorAgentOptions config,
        ChatSession session,
        string prompt)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = session.WorkspacePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string argument in BuildArguments(config, session, prompt))
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private async IAsyncEnumerable<AgentEvent> ReadEventsAsync(
        Process process,
        int timeoutSeconds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        Task<string> stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

        AgentErrorEvent? readError = null;

        while (true)
        {
            string? line;
            try
            {
                line = await process.StandardOutput.ReadLineAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                readError = new AgentErrorEvent("Agent.Timeout", "Cursor agent timed out.");
                break;
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

        if (readError is not null)
        {
            yield return readError;
            yield break;
        }

        string stderr = await stderrTask;
        if (!string.IsNullOrWhiteSpace(stderr) && process.ExitCode != 0)
        {
            logger.LogWarning("Cursor agent stderr for chat process: {StdErr}", stderr);
        }
    }
}
