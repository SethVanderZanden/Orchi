param(
    [int]$RuntimePort = 0,
    [int]$DevPort = 0,
    [string]$RuntimeRoot = '',
    [switch]$ResetData,
    [switch]$Installer,
    [switch]$SkipDesktopBuild,
    [switch]$SkipApiPublish
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptRoot 'lib\OrchiRuntimeConfig.ps1')

$config = Get-OrchiRuntimeConfig -ScriptRoot $scriptRoot -RuntimePort $RuntimePort -DevPort $DevPort -RuntimeRoot $RuntimeRoot -ResetData:$ResetData

Write-Host "Orchi runtime setup"
Write-Host "  Repo:          $($config.RepoRoot)"
Write-Host "  Runtime root:  $($config.RuntimeRoot)"
Write-Host "  Runtime port:  $($config.RuntimePort)"
Write-Host "  Dev port:      $($config.DevPort)"

if ($config.ResetData -and (Test-Path -LiteralPath $config.RuntimeDataDir)) {
    Write-Host "Resetting runtime data at $($config.RuntimeDataDir)"
    Remove-Item -LiteralPath $config.RuntimeDataDir -Recurse -Force
}

New-Item -ItemType Directory -Path $config.RuntimeDataDir -Force | Out-Null
New-Item -ItemType Directory -Path $config.GeneratedDir -Force | Out-Null

if (-not $SkipApiPublish) {
    if (Test-Path -LiteralPath $config.BuildTempDir) {
        Remove-Item -LiteralPath $config.BuildTempDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $config.ApiPublishDir -Force | Out-Null

    Write-Host "Publishing API (self-contained $($config.RuntimeIdentifier))..."
    Push-Location $config.RepoRoot
    try {
        dotnet publish (Join-Path $config.RepoRoot 'src\API\Orchi.Api.csproj') `
            -p:PublishProfile=RuntimeBundle `
            -p:RuntimeIdentifier=$($config.RuntimeIdentifier) `
            -c $config.BuildConfiguration `
            -o $config.ApiPublishDir
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }
}
elseif (-not (Test-Path -LiteralPath $config.ApiPublishDir)) {
    throw "API publish output not found at $($config.ApiPublishDir). Run without -SkipApiPublish first."
}

if (-not $SkipDesktopBuild) {
    $runtimeApiUrl = "http://localhost:$($config.RuntimePort)"
    $env:VITE_API_BASE_URL = $runtimeApiUrl
    $env:ORCHI_RUNTIME_PORT = "$($config.RuntimePort)"

    Write-Host "Building desktop (API URL: $runtimeApiUrl)..."

    Push-Location $config.DesktopDir
    try {
        npm run build
        if ($LASTEXITCODE -ne 0) {
            throw "desktop build failed with exit code $LASTEXITCODE"
        }

        if ($Installer) {
            npx electron-builder --win
        }
        else {
            npx electron-builder --dir
        }

        if ($LASTEXITCODE -ne 0) {
            throw "electron-builder failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }
}

$desktopUnpackedDir = Resolve-DesktopUnpackedDir -DesktopDir $config.DesktopDir
Write-Host "  Desktop build: $desktopUnpackedDir"

Write-Host "Deploying runtime to $($config.RuntimeRoot)..."
if (Test-Path -LiteralPath $config.RuntimeRoot) {
    Remove-Item -LiteralPath $config.RuntimeRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $config.RuntimeRoot -Force | Out-Null
Copy-Item -Path (Join-Path $desktopUnpackedDir '*') -Destination $config.RuntimeRoot -Recurse -Force

$devEnvPath = Join-Path $config.GeneratedDir 'dev.env'
$devEnv = @{
    ASPNETCORE_URLS = "http://localhost:$($config.DevPort)"
    'ConnectionStrings__DefaultConnection' = "Data Source=$($config.DevDbPath)"
    VITE_API_BASE_URL = "http://localhost:$($config.DevPort)"
    ORCHI_API_URL = "http://localhost:$($config.DevPort)"
}

Write-DotEnvFile -Path $devEnvPath -Values $devEnv

$devEnvLocalPath = Join-Path $config.DesktopDir '.env.development.local'
Write-DotEnvFile -Path $devEnvLocalPath -Values @{
    VITE_API_BASE_URL = "http://localhost:$($config.DevPort)"
}

$manifestPath = Join-Path $config.GeneratedDir 'manifest.json'
$manifest = @{
    generatedAt = (Get-Date).ToString('o')
    gitCommit = Get-GitCommitHash -RepoRoot $config.RepoRoot
    runtimeRoot = $config.RuntimeRoot
    runtimeExecutable = $config.RuntimeExecutable
    runtimePort = $config.RuntimePort
    devPort = $config.DevPort
    devDbPath = $config.DevDbPath
    runtimeDbPath = $config.RuntimeDbPath
}

$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding utf8

Write-Host ""
Write-Host "Setup complete."
Write-Host "  Runtime exe:   $($config.RuntimeExecutable)"
Write-Host "  Dev env file:  $devEnvPath"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  npm run start:runtime   # stable app (port $($config.RuntimePort))"
Write-Host "  npm run dev             # repo dev (port $($config.DevPort))"
