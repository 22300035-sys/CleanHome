param([string]$Email, [switch]$SendTest)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$privateRoot = Join-Path $repoRoot '.local-services/cloudflare'
New-Item -ItemType Directory -Path $privateRoot -Force | Out-Null
if (!$Email) { $Email = Read-Host 'Correo Gmail o Google Workspace remitente' }
$address = New-Object System.Net.Mail.MailAddress($Email)
if ($address.Address -ne $Email.Trim() -or $Email -match '[\r\n]') { throw 'Introduce solo la direccion de correo, sin nombre ni saltos de linea.' }
$secure = Read-Host 'Contrasena de aplicacion Google (no la contrasena normal)' -AsSecureString
$credential = New-Object System.Management.Automation.PSCredential($Email,$secure)
$appPassword = $credential.GetNetworkCredential().Password.Replace(' ','')
if ($appPassword.Length -ne 16) { throw 'Google genera una contrasena de aplicacion de 16 caracteres. No uses tu contrasena normal.' }
$acceptedAt = $null
if ($SendTest) {
    # Explicit switch authorizes a single test message to the sender's own account.
    $smtp = New-Object System.Net.Mail.SmtpClient('smtp.gmail.com',587)
    $smtp.EnableSsl = $true
    $smtp.Timeout = 20000
    $smtp.Credentials = New-Object System.Net.NetworkCredential($Email,$appPassword)
    $message = New-Object System.Net.Mail.MailMessage($Email,$Email,'CleanHome - prueba SMTP','Prueba de conexion de CleanHome. Gmail acepto la autenticacion y el envio de este mensaje. No incluye contrasenas ni codigos de acceso.')
    try { $smtp.Send($message); $acceptedAt = [DateTime]::UtcNow.ToString('o') }
    finally { $message.Dispose(); $smtp.Dispose() }
}
$protected = (ConvertTo-SecureString $appPassword -AsPlainText -Force) | ConvertFrom-SecureString
@{ Host='smtp.gmail.com';Port=587;Username=$Email;From=$Email;PasswordProtected=$protected;SmtpAcceptedAt=$acceptedAt } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $privateRoot 'email.json') -Encoding UTF8
$appPassword=$null
Write-Output 'Configuracion SMTP guardada cifrada para este usuario Windows, fuera de Git.'
if ($SendTest) { Write-Output 'Gmail acepto el correo de prueba. Comprueba Recibidos y Spam.' }
else { Write-Output 'Todavia no se comprobo el envio. Usa -SendTest antes de publicar cuentas reales.' }
