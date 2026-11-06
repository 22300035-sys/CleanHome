param([Parameter(Mandatory=$true)][ValidateSet('Stripe','MercadoPago')][string]$Provider,[switch]$KeyOnly)
$ErrorActionPreference='Stop'
if($KeyOnly -and $Provider -ne 'Stripe'){throw '-KeyOnly solo admite Stripe.'}
$root=Join-Path (Split-Path -Parent $PSScriptRoot) '.local-services/cloudflare'
$path=Join-Path $root 'payments.json'
$data=@{}
if(Test-Path $path){$existing=Get-Content $path -Raw|ConvertFrom-Json;foreach($p in $existing.PSObject.Properties){$data[$p.Name]=$p.Value}}
if($Provider -eq 'Stripe'){
    $key=Read-Host 'Stripe SECRET KEY del sandbox (sk_test_...)' -AsSecureString
    $plain=(New-Object System.Management.Automation.PSCredential('local',$key)).GetNetworkCredential().Password
    if(!$plain.StartsWith('sk_test_')){throw 'Solo se admiten claves Stripe de prueba sk_test_.'}
    if($KeyOnly){
        # Una clave nueva invalida el webhook previo: puede pertenecer a otro sandbox.
        $data.Stripe=@{KeyProtected=($key|ConvertFrom-SecureString)}
    }else{
    $secret=Read-Host 'Signing secret del webhook de ese sandbox (whsec_...)' -AsSecureString
    $check=(New-Object System.Management.Automation.PSCredential('local',$secret)).GetNetworkCredential().Password
    if(!$check.StartsWith('whsec_')){throw 'El signing secret debe comenzar con whsec_.'}
    $data.Stripe=@{KeyProtected=($key|ConvertFrom-SecureString);WebhookProtected=($secret|ConvertFrom-SecureString)}
    }
}else{
    $confirmation=Read-Host 'Confirma que usaras credenciales de la aplicacion en PRUEBA (no del comercio real): escribe PRUEBAS'
    if($confirmation -cne 'PRUEBAS'){throw 'Configuracion cancelada: no se habilitaran cobros reales.'}
    $key=Read-Host 'Access Token del vendedor de prueba de Mercado Pago' -AsSecureString
    $secret=Read-Host 'Firma secreta del webhook de esa aplicacion de prueba' -AsSecureString
    foreach($item in @($key,$secret)){if($item.Length -eq 0){throw 'Falta una credencial.'}}
    $data.MercadoPago=@{KeyProtected=($key|ConvertFrom-SecureString);WebhookProtected=($secret|ConvertFrom-SecureString);TestAccountConfirmed=$true}
}
$plain=$null;$check=$null
New-Item -ItemType Directory $root -Force|Out-Null
$data|ConvertTo-Json -Depth 5|Set-Content $path -Encoding UTF8
if($KeyOnly){Write-Output 'Clave Stripe guardada cifrada. Falta configurar el webhook; los pagos aun no estan habilitados.'}
else{Write-Output "$Provider guardado cifrado para este usuario. Aun NO validado con el proveedor. Reinicia la API para cargarlo."}
