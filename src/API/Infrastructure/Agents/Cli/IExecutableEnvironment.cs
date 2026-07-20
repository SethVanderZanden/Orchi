namespace Orchi.Api.Infrastructure.Agents.Cli;

internal interface IExecutableEnvironment
{
    AgentCliHostPlatform HostPlatform { get; }

    AgentCliHostArchitecture HostArchitecture { get; }

    bool IsWindows => HostPlatform == AgentCliHostPlatform.Windows;

    string? GetEnvironmentVariable(string name);

    string ExpandEnvironmentVariables(string value);

    bool FileExists(string path);

    bool DirectoryExists(string path);

    IReadOnlyList<string> GetDirectories(string path);

    IReadOnlyList<string> GetPathDirectories();

    IReadOnlyList<string> GetPathExtensions();
}

internal sealed class ExecutableEnvironment : IExecutableEnvironment
{
    public static IExecutableEnvironment Current { get; } = new ExecutableEnvironment();

    public AgentCliHostPlatform HostPlatform { get; } = AgentCliHostDetector.DetectPlatform();

    public AgentCliHostArchitecture HostArchitecture { get; } = AgentCliHostDetector.DetectArchitecture();

    public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

    public string ExpandEnvironmentVariables(string value) => Environment.ExpandEnvironmentVariables(value);

    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IReadOnlyList<string> GetDirectories(string path) =>
        Directory.Exists(path) ? Directory.GetDirectories(path) : [];

    public IReadOnlyList<string> GetPathDirectories()
    {
        var directories = new List<string>();

        AddPathDirectories(directories, Environment.GetEnvironmentVariable("PATH"), Path.PathSeparator);

        if (HostPlatform == AgentCliHostPlatform.Windows)
        {
            AddPathDirectories(
                directories,
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User),
                ';');
            AddPathDirectories(
                directories,
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine),
                ';');
        }
        else
        {
            foreach (string directory in AgentCliLoginPathEnricher.GetExtraPathDirectories(this))
            {
                directories.Add(directory);
            }
        }

        return directories
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> GetPathExtensions()
    {
        if (HostPlatform != AgentCliHostPlatform.Windows)
        {
            return [];
        }

        string? pathExt = Environment.GetEnvironmentVariable("PATHEXT");

        if (string.IsNullOrWhiteSpace(pathExt))
        {
            return [".COM", ".EXE", ".BAT", ".CMD"];
        }

        return pathExt
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddPathDirectories(
        ICollection<string> directories,
        string? pathValue,
        char separator)
    {
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return;
        }

        foreach (string directory in pathValue.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            directories.Add(directory.Trim('"'));
        }
    }
}
