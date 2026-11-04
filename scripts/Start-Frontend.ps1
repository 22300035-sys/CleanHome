param([switch]$Preview)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$frontendRoot = Join-Path $repoRoot 'frontend'
$node = Get-Command node -ErrorAction SilentlyContinue
if (!$node) { throw 'Instala Node.js 24 o agrega node al PATH.' }
if (!(Test-Path (Join-Path $frontendRoot 'node_modules'))) { throw 'Instala primero las dependencias con npm ci dentro de frontend.' }
$port = if ($Preview) { 5174 } else { 5173 }
Write-Output "Demostración: http://127.0.0.1:$port/inicio?demo=1"
Write-Output "Acceso a la API: http://127.0.0.1:$port/acceso"
Push-Location $frontendRoot
try {
    if ($Preview) {
        if (!(Test-Path dist/index.html)) { throw 'Ejecuta npm run build dentro de frontend antes de usar -Preview.' }
        & $node.Source node_modules/vite/bin/vite.js preview --host 127.0.0.1 --port $port
    } else { & $node.Source node_modules/vite/bin/vite.js --host 127.0.0.1 --port $port }
} finally { Pop-Location }
