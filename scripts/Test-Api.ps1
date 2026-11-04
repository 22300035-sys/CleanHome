param([string]$DotnetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    if ([string]::IsNullOrWhiteSpace($env:CLEANHOME_TEST_CONNECTION_STRING)) {
        throw 'Configura CLEANHOME_TEST_CONNECTION_STRING hacia PostgreSQL local dedicado (cleanhome_test).'
    }
    & $DotnetPath restore backend/CleanHome.slnx
    if ($LASTEXITCODE -ne 0) { throw 'Restore falló.' }
    & $DotnetPath build backend/CleanHome.slnx -c Release --no-restore --warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Build falló.' }
    & $DotnetPath test backend/CleanHome.slnx -c Release --no-build --logger trx --results-directory TestResults
    if ($LASTEXITCODE -ne 0) { throw 'Las pruebas fallaron.' }
} finally { Pop-Location }
