namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Testable filesystem / PATH seam for CLI executable discovery.
/// </summary>
internal interface IExecutableEnvironment
{
    bool IsWindows { get; }

    string? GetEnvironmentVariable(string name);

    string ExpandEnvironmentVariables(string value);

    bool FileExists(string path);

    bool DirectoryExists(string path);

    IReadOnlyList<string> GetDirectories(string path);

    IReadOnlyList<string> GetPathDirectories();

    IReadOnlyList<string> GetPathExtensions();
}
