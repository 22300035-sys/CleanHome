param([ValidateSet('All','Api','Tunnel')][string]$Service='All',[switch]$Foreground)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$root=Join-Path $repo '.local-services/cloudflare'
$s=Get-Content (Join-Path $repo '.local-services/settings.json') -Raw|ConvertFrom-Json
$public=Get-Content (Join-Path $root 'hosting.json') -Raw|ConvertFrom-Json
function Start-PublicWorker($name) {
    $record=Join-Path $root "$name.process.json"
    if(Test-Path $record){$p=Get-Content $record -Raw|ConvertFrom-Json;$running=Get-Process -Id $p.ProcessId -ErrorAction SilentlyContinue;if($running -and [Math]::Abs(($running.StartTime.ToUniversalTime() - ([DateTime]$p.StartTimeUtc).ToUniversalTime()).TotalSeconds) -lt 1){return}}
    $command="& '$($PSCommandPath.Replace("'","''"))' -Service '$name' -Foreground"
    $encoded=[Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))
    $p=Start-Process powershell.exe -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-EncodedCommand',$encoded -WindowStyle Hidden -WorkingDirectory $repo -RedirectStandardOutput (Join-Path $root "$name.out.log") -RedirectStandardError (Join-Path $root "$name.err.log") -PassThru
    @{ProcessId=$p.Id;StartTimeUtc=$p.StartTime.ToUniversalTime().ToString('o')}|ConvertTo-Json|Set-Content $record
}
if(!$Foreground){
    & (Join-Path $PSScriptRoot 'Start-CleanHome.ps1') -Service Database
    if($Service -in 'All','Api'){Start-PublicWorker 'Api'}
    $ready=$false
    for($i=0;$i -lt 40;$i++){try{$r=Invoke-WebRequest 'http://127.0.0.1:5190/health/ready' -Headers @{'X-Forwarded-Proto'='https'} -UseBasicParsing -TimeoutSec 2;if($r.StatusCode -eq 200){$ready=$true;break}}catch{};Start-Sleep 1}
    if(!$ready){throw 'La API publica no esta lista. Revisa Api.err.log; no se iniciara el tunel.'}
    if($Service -in 'All','Tunnel'){Start-PublicWorker 'Tunnel'}
    Write-Output 'Servidor temporal: https://cleanhome.dorms21.win'
    return
}
if($Service -eq 'Api'){
    . (Join-Path $PSScriptRoot 'Import-SmtpConfiguration.ps1')
    $env:Email__PublicBaseUrl='https://cleanhome.dorms21.win'
    . (Join-Path $PSScriptRoot 'Import-TestPayments.ps1')
    $secure=ConvertTo-SecureString $public.ConnectionStringProtected
    $env:ConnectionStrings__CleanHome=(New-Object System.Management.Automation.PSCredential('local',$secure)).GetNetworkCredential().Password
    $env:DOTNET_ROOT=Split-Path $s.DotnetPath
    $env:ASPNETCORE_ENVIRONMENT='Production'
    $env:ASPNETCORE_URLS='http://127.0.0.1:5190'
    $env:AllowedHosts='cleanhome.dorms21.win;cleanhome-laptop.dorms21.win;127.0.0.1;localhost'
    $env:OpenApi__Enabled='false'
    $env:Hosting__ServeFrontend='true'
    $env:Hosting__FrontendRoot=Join-Path $root 'frontend'
    $env:Hosting__TrustLoopbackProxy='true'
    $env:Hosting__CanonicalOrigin='https://cleanhome.dorms21.win'
    $env:Hosting__LegacyHost='cleanhome-laptop.dorms21.win'
    $env:Storage__Documents=Join-Path $root 'documents'
    $env:Storage__DataProtectionKeys=Join-Path $root 'keys'
    Set-Location (Join-Path $root 'publish')
    & $s.DotnetPath './CleanHome.Api.dll'
    exit $LASTEXITCODE
}
if($Service -eq 'Tunnel'){
    & $public.CloudflaredPath tunnel --config (Join-Path $root 'tunnel.yml') run
    exit $LASTEXITCODE
}
