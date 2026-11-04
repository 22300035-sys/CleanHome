param([switch]$Disable)
$ErrorActionPreference = 'Stop'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$entryName = 'CleanHomeLocal'
if ($Disable) {
    Remove-ItemProperty -LiteralPath $runKey -Name $entryName -ErrorAction SilentlyContinue
    Write-Output 'Arranque automatico de CleanHome desactivado.'
    exit
}
$startScript = Join-Path $PSScriptRoot 'Start-CleanHome.ps1'
$hostExe = (Get-Process -Id $PID).Path
$command = "& '$($startScript.Replace("'", "''"))'"
$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))
$entryValue = '"' + $hostExe + '" -NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -EncodedCommand ' + $encoded
New-Item -Path $runKey -Force | Out-Null
$existing = (Get-ItemProperty -LiteralPath $runKey -Name $entryName -ErrorAction SilentlyContinue).$entryName
if ($existing -and $existing -ne $entryValue) { throw 'Ya existe CleanHomeLocal con otro comando. Revisalo antes de sustituirlo.' }
New-ItemProperty -LiteralPath $runKey -Name $entryName -PropertyType String -Value $entryValue -Force | Out-Null
Write-Output 'CleanHome iniciara al entrar a Windows con este usuario. No es un servicio de produccion.'
