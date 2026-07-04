$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptRoot 'lib\OrchiRuntimeConfig.ps1')

$config = Get-OrchiRuntimeConfig -ScriptRoot $scriptRoot
$manifestPath = Join-Path $config.GeneratedDir 'manifest.json'

if (-not (Test-Path -LiteralPath $config.RuntimeExecutable)) {
    if (Test-Path -LiteralPath $manifestPath) {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        if ($manifest.runtimeExecutable -and (Test-Path -LiteralPath $manifest.runtimeExecutable)) {
            $config.RuntimeExecutable = $manifest.runtimeExecutable
        }
    }
}

if (-not (Test-Path -LiteralPath $config.RuntimeExecutable)) {
    throw "Runtime executable not found at $($config.RuntimeExecutable). Run npm run setup:runtime first."
}

Write-Host "Starting Orchi runtime: $($config.RuntimeExecutable)"
Start-Process -FilePath $config.RuntimeExecutable
