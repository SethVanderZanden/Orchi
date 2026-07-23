using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using Orchi.Api.Infrastructure.Agents.Cli;
using Orchi.Api.Infrastructure.Agents.Models;
using Orchi.Api.Infrastructure.Caching;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal sealed class CursorAgentModelListProvider(
    IOptions<CursorAgentOptions> options,
    OrchiHybridCacheService cache,
    ILogger<CursorAgentModelListProvider> logger) : IAgentModelListProvider
{
    public string AgentId => "cursor";

    public async Task<IReadOnlyList<AgentModelListEntry>> FetchModelsAsync(CancellationToken cancellationToken)
    {
        CursorAgentOptions config = options.Value;

        CursorAgentExecutableResolver.ResolveResult resolveResult =
            await ResolveExecutableAsync(config, cancellationToken);

        if (!resolveResult.Success || resolveResult.Launch is null)
        {
            throw new InvalidOperationException(resolveResult.ErrorMessage ?? "Unable to resolve Cursor agent executable.");
        }

        AgentLaunchSpec launch = resolveResult.Launch;
        ProcessStartInfo startInfo = BuildStartInfo(launch, config);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start Cursor agent executable '{launch.ExecutablePath}'.");

        using var registration = cancellationToken.Register(() => TryKillProcess(process));

        string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        string stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

        if (!process.HasExited)
        {
            await process.WaitForExitAsync(cancellationToken);
        }

        if (process.ExitCode != 0)
        {
            logger.LogWarning(
                "Cursor agent --list-models exited with code {ExitCode}. Stderr: {StdErr}",
                process.ExitCode,
                stderr);

            throw new InvalidOperationException(
                $"Cursor agent --list-models failed with exit code {process.ExitCode}.");
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            logger.LogDebug("Cursor agent --list-models stderr: {StdErr}", stderr);
        }

        return CursorModelListParser.Parse(stdout);
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

    private static ProcessStartInfo BuildStartInfo(AgentLaunchSpec launch, CursorAgentOptions config)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = launch.ExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(launch.EntryScript))
        {
            startInfo.ArgumentList.Add(launch.EntryScript);
        }

        startInfo.ArgumentList.Add("--list-models");
        return startInfo;
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }
}
