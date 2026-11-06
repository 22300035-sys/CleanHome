$ErrorActionPreference='Stop'
$root=Join-Path (Split-Path -Parent $PSScriptRoot) '.local-services/cloudflare'
foreach($name in 'Tunnel','Api'){
    $record=Join-Path $root "$name.process.json"
    if(!(Test-Path $record)){continue}
    $saved=Get-Content $record -Raw|ConvertFrom-Json
    $p=Get-Process -Id $saved.ProcessId -ErrorAction SilentlyContinue
    if($p -and [Math]::Abs(($p.StartTime.ToUniversalTime() - ([DateTime]$saved.StartTimeUtc).ToUniversalTime()).TotalSeconds) -lt 1){
        & taskkill.exe /PID $p.Id /T /F | Out-Null
        if($LASTEXITCODE -ne 0){throw "No se pudo detener $name"}
    }
}
Write-Output 'Servidor publico detenido. PostgreSQL y servicios de desarrollo se conservan.'
