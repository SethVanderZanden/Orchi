$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptRoot 'lib\OrchiRuntimeConfig.ps1')

$config = Get-OrchiRuntimeConfig -ScriptRoot $scriptRoot
$devEnvPath = Join-Path $config.GeneratedDir 'dev.env'
$usingIsolatedDev = Test-Path -LiteralPath $devEnvPath

if ($usingIsolatedDev) {
    Import-DotEnvFile -Path $devEnvPath
    Write-Host "Dev stack on port $($config.DevPort) (runtime can stay open on $($config.RuntimePort))"
}
else {
    Write-Host "Dev stack on port $($config.RuntimePort)"
    Write-Host "Tip: run npm run setup:runtime once to dev in parallel with npm run start:runtime"
}

Push-Location $config.RepoRoot
try {
    npx concurrently -n api,desktop -c blue,green `
        "dotnet run --project src/API" `
        "npm run dev --prefix src/desktop"
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
