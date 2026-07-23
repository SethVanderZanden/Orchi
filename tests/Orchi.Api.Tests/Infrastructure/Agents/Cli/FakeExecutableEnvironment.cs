using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Tests.Infrastructure.Agents.Cli;

internal sealed class FakeExecutableEnvironment : IExecutableEnvironment
{
    public bool IsWindows { get; init; }

    public HashSet<string> ExistingFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> ExistingDirectories { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> PathDirectories { get; } = [];

    public Dictionary<string, string> EnvironmentVariables { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string? GetEnvironmentVariable(string name) =>
        EnvironmentVariables.TryGetValue(name, out string? value) ? value : null;

    public string ExpandEnvironmentVariables(string value)
    {
        string expanded = value;

        foreach ((string key, string envValue) in EnvironmentVariables)
        {
            expanded = expanded.Replace($"%{key}%", envValue, StringComparison.OrdinalIgnoreCase);
        }

        return expanded;
    }

    public bool FileExists(string path) => ExistingFiles.Contains(path);

    public bool DirectoryExists(string path) =>
        ExistingDirectories.Contains(path) || Directory.Exists(path);

    public IReadOnlyList<string> GetDirectories(string path)
    {
        if (Directory.Exists(path))
        {
            return Directory.GetDirectories(path);
        }

        return [];
    }

    public IReadOnlyList<string> GetPathDirectories() => PathDirectories;

    public IReadOnlyList<string> GetPathExtensions() => [".COM", ".EXE", ".BAT", ".CMD"];
}
