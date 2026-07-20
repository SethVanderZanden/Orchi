namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Well-known CLI install directories per host platform. Always searched in addition to PATH.
/// </summary>
internal static class AgentCliKnownDirectories
{
    public static IEnumerable<string> For(IExecutableEnvironment environment)
    {
        var directories = new List<string>();

        switch (environment.HostPlatform)
        {
            case AgentCliHostPlatform.Windows:
                AddWindows(directories, environment);
                break;
            case AgentCliHostPlatform.MacOS:
                AddMacOS(directories, environment);
                break;
            case AgentCliHostPlatform.Linux:
                AddLinux(directories, environment);
                break;
        }

        return directories
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Select(directory => environment.ExpandEnvironmentVariables(directory.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static void AddWindows(List<string> directories, IExecutableEnvironment environment)
    {
        string? appData = environment.GetEnvironmentVariable("APPDATA");
        string? localAppData = environment.GetEnvironmentVariable("LOCALAPPDATA");
        string? programFiles = environment.GetEnvironmentVariable("ProgramFiles");

        if (!string.IsNullOrWhiteSpace(appData))
        {
            directories.Add(Path.Combine(appData, "npm"));
        }

        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            directories.Add(Path.Combine(localAppData, "Programs", "nodejs"));
            directories.Add(Path.Combine(localAppData, "Volta", "bin"));
            directories.Add(Path.Combine(localAppData, "pnpm"));
        }

        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            directories.Add(Path.Combine(programFiles, "nodejs"));
        }
    }

    private static void AddMacOS(List<string> directories, IExecutableEnvironment environment)
    {
        string home = environment.GetEnvironmentVariable("HOME") ?? string.Empty;

        directories.Add("/opt/homebrew/bin");
        directories.Add("/usr/local/bin");

        if (!string.IsNullOrWhiteSpace(home))
        {
            directories.Add(Path.Combine(home, ".npm-global", "bin"));
            directories.Add(Path.Combine(home, "Library", "pnpm"));
            directories.Add(Path.Combine(home, ".volta", "bin"));
            directories.Add(Path.Combine(home, ".local", "bin"));
        }
    }

    private static void AddLinux(List<string> directories, IExecutableEnvironment environment)
    {
        string home = environment.GetEnvironmentVariable("HOME") ?? string.Empty;

        directories.Add("/usr/bin");
        directories.Add("/usr/local/bin");
        directories.Add("/home/linuxbrew/.linuxbrew/bin");

        if (!string.IsNullOrWhiteSpace(home))
        {
            directories.Add(Path.Combine(home, ".local", "bin"));
            directories.Add(Path.Combine(home, ".npm-global", "bin"));
            directories.Add(Path.Combine(home, ".volta", "bin"));
        }
    }
}
