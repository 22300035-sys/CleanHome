param(
    [ValidateSet('All','Database','Api','Frontend')][string]$Service = 'All',
    [switch]$Foreground,
    [switch]$Build
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$stateRoot = Join-Path $repoRoot '.local-services'
$settingsPath = Join-Path $stateRoot 'settings.json'
if (!(Test-Path -LiteralPath $settingsPath)) { throw 'Falta .local-services/settings.json. Lee docs/ARRANQUE_LOCAL.md para configurar este equipo.' }
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
$logRoot = Join-Path $stateRoot 'logs'
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null

function Test-Http([string]$Url) {
    try { return (Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200 } catch { return $false }
}
function Wait-Http([string]$Url, [string]$Name) {
    for ($attempt = 0; $attempt -lt 45; $attempt++) {
        if (Test-Http $Url) { return }
        Start-Sleep -Seconds 1
    }
    throw "$Name no respondio en $Url. Revisa $logRoot; no se ejecutan migraciones automaticamente."
}
function Start-Worker([string]$Name) {
    $scriptPath = $PSCommandPath.Replace("'", "''")
    $command = "& '$scriptPath' -Service '$Name' -Foreground"
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))
    $hostExe = (Get-Process -Id $PID).Path
    $child = Start-Process -FilePath $hostExe -ArgumentList '-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-EncodedCommand',$encoded -WindowStyle Hidden -WorkingDirectory $repoRoot -RedirectStandardOutput (Join-Path $logRoot "$Name.out.log") -RedirectStandardError (Join-Path $logRoot "$Name.err.log") -PassThru
    @{ ProcessId = $child.Id; StartTimeUtc = $child.StartTime.ToUniversalTime().ToString('o'); RepoRoot = $repoRoot; Service = $Name } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stateRoot "$Name.process.json") -Encoding UTF8
}

if ($Service -eq 'Database' -or $Service -eq 'All') {
    $pgCtl = Join-Path $settings.PostgresBin 'pg_ctl.exe'
    $pgReady = Join-Path $settings.PostgresBin 'pg_isready.exe'
    if (!(Test-Path -LiteralPath $pgCtl)) { throw "No existe PostgreSQL en $($settings.PostgresBin)" }
    & $pgCtl -D $settings.PostgresData status *> $null
    if ($LASTEXITCODE -eq 0) {
        & $pgReady -h 127.0.0.1 -p $settings.DatabasePort -q
        if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL esta activo pero no responde en el puerto configurado; revisa postmaster.opts.' }
    } else {
        & $pgReady -h 127.0.0.1 -p $settings.DatabasePort -q
        if ($LASTEXITCODE -eq 0) { throw 'El puerto PostgreSQL ya esta ocupado por otra instancia; no se modificara.' }
        $postgresOptions = "-p $($settings.DatabasePort) -h 127.0.0.1"
        & $pgCtl -D $settings.PostgresData -l (Join-Path $logRoot 'postgres.log') -o $postgresOptions -w start
        if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL no pudo iniciar.' }
    }
    Write-Output "PostgreSQL: 127.0.0.1:$($settings.DatabasePort)"
}

if ($Service -eq 'Api' -or $Service -eq 'All') {
    $env:DOTNET_ROOT = Split-Path -Parent $settings.DotnetPath
    $env:DOTNET_CLI_HOME = $settings.DotnetCliHome
    $env:NUGET_PACKAGES = $settings.NugetPackages
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $apiRoot = Join-Path $repoRoot 'backend/src/CleanHome.Api'
    $apiDll = Join-Path $apiRoot 'bin/Release/net10.0/CleanHome.Api.dll'
    $apiUrl = "http://127.0.0.1:$($settings.ApiPort)"
    if ($Foreground) {
        $secure = ConvertTo-SecureString $settings.ConnectionStringProtected
        $credential = New-Object System.Management.Automation.PSCredential('local', $secure)
        $env:ConnectionStrings__CleanHome = $credential.GetNetworkCredential().Password
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        $env:ASPNETCORE_URLS = $apiUrl
        $env:OpenApi__Enabled = 'true'
        $env:Cors__AllowedOrigins__0 = "http://127.0.0.1:$($settings.FrontendPort)"
        Push-Location $apiRoot
        try { & $settings.DotnetPath $apiDll; exit $LASTEXITCODE } finally { Pop-Location }
    }
    $running = Test-Http "$apiUrl/health"
    if ($running -and $Build) { throw 'Deten el backend antes de usar -Build para evitar DLL bloqueada en Windows.' }
    if (!$running) {
        if ($Build -or !(Test-Path -LiteralPath $apiDll)) {
            Push-Location $repoRoot
            try {
                & $settings.DotnetPath build backend/CleanHome.slnx -c Release --warnaserror
                if ($LASTEXITCODE -ne 0) { throw 'La compilacion fallo; revisa la salida.' }
            } finally { Pop-Location }
        }
        Start-Worker 'Api'
    }
    Wait-Http "$apiUrl/health/ready" 'API/PostgreSQL'
    Write-Output "API: $apiUrl | Swagger: $apiUrl/swagger"
}

if ($Service -eq 'Frontend' -or $Service -eq 'All') {
    $frontendRoot = Join-Path $repoRoot 'frontend'
    $vite = Join-Path $frontendRoot 'node_modules/vite/bin/vite.js'
    if (!(Test-Path -LiteralPath $vite)) { throw 'Faltan dependencias: ejecuta npm ci dentro de frontend.' }
    $frontUrl = "http://127.0.0.1:$($settings.FrontendPort)"
    if ($Foreground) {
        $env:CLEANHOME_DEV_API_TARGET = "http://127.0.0.1:$($settings.ApiPort)"
        $env:VITE_API_BASE_URL = '/api/v1'
        Push-Location $frontendRoot
        try { & $settings.NodePath $vite --host 127.0.0.1 --port $settings.FrontendPort --strictPort; exit $LASTEXITCODE } finally { Pop-Location }
    }
    if (!(Test-Http "$frontUrl/acceso")) { Start-Worker 'Frontend' }
    Wait-Http "$frontUrl/acceso" 'Frontend'
    Write-Output "Frontend: $frontUrl/acceso | Demo: $frontUrl/inicio?demo=1"
}
