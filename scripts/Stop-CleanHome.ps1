param([switch]$KeepDatabase)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$stateRoot = Join-Path $repoRoot '.local-services'
foreach ($service in @('Frontend','Api')) {
    $recordPath = Join-Path $stateRoot "$service.process.json"
    if (!(Test-Path -LiteralPath $recordPath)) { Write-Output "$service no fue iniciado por este script; no se detiene."; continue }
    $record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
    if ($record.RepoRoot -ne $repoRoot -or $record.Service -ne $service) { throw "Registro de proceso incorrecto: $service" }
    $process = Get-Process -Id $record.ProcessId -ErrorAction SilentlyContinue
    if ($process) {
        $recordedTime = ([DateTime]$record.StartTimeUtc).ToUniversalTime()
        if ($process.StartTime.ToUniversalTime().Ticks -ne $recordedTime.Ticks) { throw "El PID fue reutilizado; no se detendra: $service" }
        & taskkill.exe /PID $record.ProcessId /T /F | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "No se pudo detener $service" }
    }
    Remove-Item -LiteralPath $recordPath
    Write-Output "$service detenido."
}
if (!$KeepDatabase) {
    $settings = Get-Content -LiteralPath (Join-Path $stateRoot 'settings.json') -Raw | ConvertFrom-Json
    $pgCtl = Join-Path $settings.PostgresBin 'pg_ctl.exe'
    & $pgCtl -D $settings.PostgresData status *> $null
    if ($LASTEXITCODE -eq 0) {
        & $pgCtl -D $settings.PostgresData -m fast -w stop
        if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL no pudo detenerse limpiamente.' }
    }
    Write-Output 'PostgreSQL detenido; datos conservados.'
}
