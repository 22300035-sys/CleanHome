param([string]$DotnetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    New-Item -ItemType Directory -Path TestResults -Force | Out-Null
    $output = & $DotnetPath list backend/CleanHome.slnx package --vulnerable --include-transitive --format json --output-version 1
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar la auditoría de NuGet.' }
    $json = $output -join [Environment]::NewLine
    $report = $json | ConvertFrom-Json
    Set-Content -LiteralPath TestResults/nuget-audit.json -Value $json -Encoding utf8
    if (!$report.projects -or $report.problems) { throw 'La consulta de NuGet no produjo un informe completo.' }
    $vulnerable = @(
        foreach ($project in $report.projects) {
            foreach ($framework in $project.frameworks) {
                foreach ($package in @($framework.topLevelPackages) + @($framework.transitivePackages)) {
                    if ($package.vulnerabilities) { $package.id }
                }
            }
        }
    )
    if ($vulnerable.Count -gt 0) { throw ('Dependencias con avisos: ' + ($vulnerable -join ', ')) }
    Write-Output 'NuGet: no se reportaron dependencias vulnerables en la fuente consultada.'
} finally { Pop-Location }
