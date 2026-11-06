param([string]$ConfigurationPath)
$ErrorActionPreference = 'Stop'
if (!$ConfigurationPath) { $ConfigurationPath = Join-Path (Split-Path -Parent $PSScriptRoot) '.local-services/cloudflare/email.json' }
$mail = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json
if (!$mail.SmtpAcceptedAt) { throw 'La configuracion SMTP no tiene prueba aceptada. Ejecuta Configure-DomainSmtp.ps1 -SendTest.' }
if (!$mail.Host -or !$mail.Username -or !$mail.From -or !$mail.PasswordProtected) { throw 'Configuracion SMTP incompleta.' }
$secure = ConvertTo-SecureString $mail.PasswordProtected
$credential = New-Object System.Management.Automation.PSCredential($mail.Username,$secure)
$env:Email__Host = $mail.Host
$env:Email__Port = [string]$mail.Port
$env:Email__Username = $mail.Username
$env:Email__From = $mail.From
$env:Email__Password = $credential.GetNetworkCredential().Password
Write-Output 'SMTP verificado cargado para este proceso; secreto no mostrado ni persistido en el PATH global.'
