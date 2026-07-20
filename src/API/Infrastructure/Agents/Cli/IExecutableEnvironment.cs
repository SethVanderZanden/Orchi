namespace Orchi.Api.Infrastructure.Agents.Cli;

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

internal sealed class ExecutableEnvironment : IExecutableEnvironment
{
    public static IExecutableEnvironment Current { get; } = new ExecutableEnvironment();

    public bool IsWindows => OperatingSystem.IsWindows();

    public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

    public string ExpandEnvironmentVariables(string value) => Environment.ExpandEnvironmentVariables(value);

    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IReadOnlyList<string> GetDirectories(string path) =>
        Directory.Exists(path) ? Directory.GetDirectories(path) : [];

    public IReadOnlyList<string> GetPathDirectories()
    {
        var directories = new List<string>();

        AddPathDirectories(directories, Environment.GetEnvironmentVariable("PATH"));

        if (IsWindows)
        {
            AddPathDirectories(directories, Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User));
            AddPathDirectories(directories, Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine));
        }

        return directories
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> GetPathExtensions()
    {
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

    private static void AddPathDirectories(ICollection<string> directories, string? pathValue)
    {
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return;
        }

        foreach (string directory in pathValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            directories.Add(directory);
        }
    }
}
