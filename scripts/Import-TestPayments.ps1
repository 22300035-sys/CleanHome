$path=Join-Path (Split-Path -Parent $PSScriptRoot) '.local-services/cloudflare/payments.json'
if(Test-Path $path){
    $data=Get-Content $path -Raw|ConvertFrom-Json
    function Read-ProtectedPayment($value){$secure=ConvertTo-SecureString $value;return (New-Object System.Management.Automation.PSCredential('local',$secure)).GetNetworkCredential().Password}
    $env:Payments__ReturnBaseUrl='https://cleanhome.dorms21.win'
    $env:Payments__PublicApiUrl='https://cleanhome.dorms21.win'
    if($data.Stripe -and $data.Stripe.WebhookProtected){
        $env:Payments__Stripe__SecretKey=Read-ProtectedPayment $data.Stripe.KeyProtected
        if(!$env:Payments__Stripe__SecretKey.StartsWith('sk_test_')){throw 'Stripe requiere clave de prueba.'}
        $env:Payments__Stripe__WebhookSecret=Read-ProtectedPayment $data.Stripe.WebhookProtected
    }
    if($data.MercadoPago){
        if(!$data.MercadoPago.TestAccountConfirmed){throw 'Confirma la cuenta de prueba de Mercado Pago.'}
        $env:Payments__MercadoPago__AccessToken=Read-ProtectedPayment $data.MercadoPago.KeyProtected
        $env:Payments__MercadoPago__WebhookSecret=Read-ProtectedPayment $data.MercadoPago.WebhookProtected
        $env:Payments__MercadoPago__Sandbox='true'
    }
    Write-Output 'Configuracion de pagos de prueba cargada; no implica validacion externa.'
}
