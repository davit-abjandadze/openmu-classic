# Builds and starts the dev server in one step.
#   .\run.ps1            build + start
#   .\run.ps1 -NoBuild   start the last build
#   .\run.ps1 -Reinit    build + start with a freshly initialized database
param(
    [switch]$NoBuild,
    [switch]$Reinit
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'src\Startup\MUnique.OpenMU.Startup.csproj'

# A running server locks the build output.
$running = Get-Process -Name 'MUnique.OpenMU.Startup' -ErrorAction SilentlyContinue
if ($running) {
    Write-Host 'Stopping the running server...' -ForegroundColor Yellow
    $running | Stop-Process -Force
    Start-Sleep -Seconds 1
}

# Dev database (docker container on port 5440).
if (Get-Command docker -ErrorAction SilentlyContinue) {
    $state = docker inspect -f '{{.State.Running}}' openmu-db 2>$null
    if ($state -ne 'true') {
        Write-Host 'Starting database container openmu-db...' -ForegroundColor Yellow
        docker start openmu-db | Out-Null
    }
}

if (-not $NoBuild) {
    Write-Host 'Building...' -ForegroundColor Cyan
    dotnet build $project -nologo -v:q "-clp:ErrorsOnly"
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'Build failed.' -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

$exe = Get-ChildItem -Path (Join-Path $root 'src\Startup\bin\Debug') -Recurse -Filter 'MUnique.OpenMU.Startup.exe' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $exe) {
    Write-Host 'Server exe not found - run without -NoBuild first.' -ForegroundColor Red
    exit 1
}

$env:DB_HOST = 'localhost:5440'
$env:DB_ADMIN_USER = 'postgres'
$env:DB_ADMIN_PW = 'openmu_dev'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://+:80'

$serverArgs = @('-autostart', '-resolveIp:local')
if ($Reinit) { $serverArgs += '-reinit' }

Write-Host "Starting server ($($serverArgs -join ' ')). Admin panel: http://localhost" -ForegroundColor Green
Push-Location $exe.DirectoryName
try {
    & $exe.FullName @serverArgs
}
finally {
    Pop-Location
}
