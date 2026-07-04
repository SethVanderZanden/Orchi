function Expand-RuntimePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $Path
    }

    $expanded = [Environment]::ExpandEnvironmentVariables($Path)

    if ($expanded -match '%([^%]+)%') {
        throw "Unresolved environment variable in path: $Path"
    }

    return $expanded
}

function Get-RepoRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptRoot
    )

    return (Resolve-Path (Join-Path $ScriptRoot '..')).Path
}

function Read-JsonObject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return @{}
    }

    $raw = Get-Content -LiteralPath $Path -Raw
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return @{}
    }

    $object = $raw | ConvertFrom-Json
    $hash = @{}

    foreach ($property in $object.PSObject.Properties) {
        $hash[$property.Name] = $property.Value
    }

    return $hash
}

function Merge-Hashtable {
    param(
        [hashtable]$Base,
        [hashtable]$Override
    )

    $merged = @{}

    foreach ($key in $Base.Keys) {
        $merged[$key] = $Base[$key]
    }

    foreach ($key in $Override.Keys) {
        $merged[$key] = $Override[$key]
    }

    return $merged
}

function Resolve-DesktopUnpackedDir {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DesktopDir
    )

    $candidates = @(
        (Join-Path $DesktopDir 'release\win-unpacked'),
        (Join-Path $DesktopDir 'dist\win-unpacked')
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "Desktop build output not found. Expected one of:`n  $($candidates -join "`n  ")"
}

function Get-OrchiRuntimeConfig {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptRoot,

        [int]$RuntimePort = 0,
        [int]$DevPort = 0,
        [string]$RuntimeRoot = '',
        [switch]$ResetData
    )

    $repoRoot = Get-RepoRoot -ScriptRoot $ScriptRoot
    $defaultsPath = Join-Path $ScriptRoot 'runtime.config.json'
    $localPath = Join-Path $ScriptRoot 'runtime.config.local.json'

    if (-not (Test-Path -LiteralPath $defaultsPath)) {
        throw "Missing runtime config: $defaultsPath"
    }

    $config = Read-JsonObject -Path $defaultsPath
    $local = Read-JsonObject -Path $localPath
    $config = Merge-Hashtable -Base $config -Override $local

    if ($RuntimePort -gt 0) {
        $config.runtimePort = $RuntimePort
    }

    if ($DevPort -gt 0) {
        $config.devPort = $DevPort
    }

    if (-not [string]::IsNullOrWhiteSpace($RuntimeRoot)) {
        $config.runtimeRoot = $RuntimeRoot
    }

    $runtimeRoot = Expand-RuntimePath -Path ([string]$config.runtimeRoot)
    $devDbRelative = [string]$config.devDbFile
    $devDbPath = if ([System.IO.Path]::IsPathRooted($devDbRelative)) {
        $devDbRelative
    } else {
        Join-Path $repoRoot $devDbRelative
    }

    $runtimeDataDir = Join-Path (Split-Path $runtimeRoot -Parent) 'data'
    $runtimeDbFile = if ($config.ContainsKey('runtimeDbFile') -and -not [string]::IsNullOrWhiteSpace([string]$config.runtimeDbFile)) {
        [string]$config.runtimeDbFile
    } else {
        'orchi.db'
    }

    $runtimeDbPath = Join-Path $runtimeDataDir $runtimeDbFile

    return [pscustomobject]@{
        RepoRoot = $repoRoot
        RuntimeRoot = $runtimeRoot
        RuntimeDataDir = $runtimeDataDir
        RuntimeDbPath = $runtimeDbPath
        RuntimePort = [int]$config.runtimePort
        DevPort = [int]$config.devPort
        DevDbPath = $devDbPath
        BuildConfiguration = [string]$config.buildConfiguration
        DesktopBuildTarget = [string]$config.desktopBuildTarget
        RuntimeIdentifier = [string]$config.runtimeIdentifier
        PreserveRuntimeData = [bool]$config.preserveRuntimeData
        ResetData = [bool]$ResetData
        BuildTempDir = Join-Path $repoRoot '.build-temp'
        ApiPublishDir = Join-Path $repoRoot '.build-temp\api-publish'
        GeneratedDir = Join-Path $ScriptRoot 'generated'
        DesktopDir = Join-Path $repoRoot 'src\desktop'
        DesktopReleaseDir = Join-Path $repoRoot 'src\desktop\release\win-unpacked'
        RuntimeExecutable = Join-Path $runtimeRoot 'Orchi.exe'
    }
}

function Get-DesktopUnpackedDir {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptRoot
    )

    $config = Get-OrchiRuntimeConfig -ScriptRoot $ScriptRoot
    return Resolve-DesktopUnpackedDir -DesktopDir $config.DesktopDir
}

function Write-DotEnvFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [hashtable]$Values
    )

    $directory = Split-Path $Path -Parent
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $lines = foreach ($key in ($Values.Keys | Sort-Object)) {
        "$key=$($Values[$key])"
    }

    Set-Content -LiteralPath $Path -Value $lines -Encoding utf8
}

function Import-DotEnvFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing env file: $Path"
    }

    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) {
            return
        }

        $parts = $line -split '=', 2
        if ($parts.Count -ne 2) {
            return
        }

        Set-Item -Path "Env:$($parts[0])" -Value $parts[1]
    }
}

function Get-GitCommitHash {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    Push-Location $RepoRoot
    try {
        $hash = git rev-parse --short HEAD 2>$null
        if ($LASTEXITCODE -ne 0) {
            return 'unknown'
        }

        return $hash.Trim()
    }
    finally {
        Pop-Location
    }
}
