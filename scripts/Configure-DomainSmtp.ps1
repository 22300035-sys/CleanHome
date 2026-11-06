param(
    [string]$SmtpHost = 'smtp.resend.com',
    [ValidateSet(587,2587)][int]$Port = 587,
    [string]$Username = 'resend',
    [string]$From = 'notificaciones@dorms21.win',
    [switch]$SendTest
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$privateRoot = Join-Path $repoRoot '.local-services/cloudflare'
$sender = New-Object System.Net.Mail.MailAddress($From)
if ($sender.Address -ne $From.Trim() -or $sender.Host -ne 'dorms21.win') { throw 'Usa una direccion del dominio dorms21.win verificado en tu proveedor.' }
$secret = Read-Host 'Clave SMTP del proveedor (en Resend: API key de solo envio)' -AsSecureString
$credential = New-Object System.Management.Automation.PSCredential($Username,$secret)
$password = $credential.GetNetworkCredential().Password
if ([string]::IsNullOrWhiteSpace($password)) { throw 'Falta la clave SMTP.' }
$acceptedAt = $null
if ($SendTest) {
    $recipientText = Read-Host 'Tu correo para recibir una unica prueba'
    $recipient = New-Object System.Net.Mail.MailAddress($recipientText)
    $smtp = New-Object System.Net.Mail.SmtpClient($SmtpHost,$Port)
    $smtp.EnableSsl = $true
    $smtp.Timeout = 20000
    $smtp.Credentials = New-Object System.Net.NetworkCredential($Username,$password)
    $message = New-Object System.Net.Mail.MailMessage($sender,$recipient)
    $message.Subject = 'CleanHome - prueba SMTP del dominio'
    $message.Body = 'Prueba solicitada de CleanHome. El servidor SMTP acepto el envio desde dorms21.win. Este mensaje no contiene claves ni codigos de acceso.'
    try { $smtp.Send($message); $acceptedAt = [DateTime]::UtcNow.ToString('o') }
    finally { $message.Dispose(); $smtp.Dispose() }
}
New-Item -ItemType Directory -Path $privateRoot -Force | Out-Null
@{Provider='DomainSmtp';Host=$SmtpHost;Port=$Port;Username=$Username;From=$From;PasswordProtected=($secret | ConvertFrom-SecureString);SmtpAcceptedAt=$acceptedAt} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $privateRoot 'email.json') -Encoding UTF8
$password=$null
Write-Output 'SMTP del dominio guardado cifrado para este usuario Windows, fuera de Git.'
if ($SendTest) { Write-Output 'El proveedor acepto la prueba; revisa Recibidos y Spam. La entrega no se confirma solo por guardar la configuracion.' }
else { Write-Output 'Falta verificar envio. Usa -SendTest antes de publicar cuentas reales.' }
