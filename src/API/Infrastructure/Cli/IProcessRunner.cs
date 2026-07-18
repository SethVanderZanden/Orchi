namespace Orchi.Api.Infrastructure.Cli;

public sealed record ProcessRunResult(
    int ExitCode,
    string StdOut,
    string StdErr,
    bool TimedOut = false)
{
    public bool Succeeded => ExitCode == 0 && !TimedOut;

    public string CombinedOutput
    {
        get
        {
            if (string.IsNullOrWhiteSpace(StdErr))
            {
                return StdOut.Trim();
            }

            if (string.IsNullOrWhiteSpace(StdOut))
            {
                return StdErr.Trim();
            }

            return $"{StdOut.Trim()}\n{StdErr.Trim()}";
        }
    }
}

public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken,
        int? timeoutMs = null);
}
