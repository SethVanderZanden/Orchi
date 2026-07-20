namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Host OS for CLI discovery. Auto-detected — never configured by the user.
/// </summary>
internal enum AgentCliHostPlatform
{
    Unknown = 0,
    Windows = 1,
    MacOS = 2,
    Linux = 3
}

/// <summary>
/// How a CLI appears to have been installed, inferred from the resolved path.
/// </summary>
internal enum AgentCliInstallKind
{
    Unknown = 0,
    NpmGlobal = 1,
    Homebrew = 2,
    NativeInstaller = 3,
    Volta = 4,
    Pnpm = 5
}

internal enum AgentCliHostArchitecture
{
    Unknown = 0,
    X64 = 1,
    Arm64 = 2
}
