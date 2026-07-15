[CmdletBinding()]
param(
    [switch]$Build
)

$ErrorActionPreference = 'Stop'
$services = @(
    'vitrin-auth',
    'vitrin-product',
    'vitrin-voting',
    'vitrin-comment',
    'vitrin-notification',
    'vitrin-analytics',
    'vitrin-ai'
)

if ($Build) {
    docker compose build @services
    if ($LASTEXITCODE -ne 0) { throw 'Service images could not be built.' }
}

docker compose up -d postgres
if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL could not be started.' }

for ($attempt = 0; $attempt -lt 60; $attempt++) {
    $health = docker inspect --format '{{.State.Health.Status}}' vitrin-postgres 2>$null
    if ($health -eq 'healthy') { break }
    Start-Sleep -Seconds 2
}

if ($health -ne 'healthy') { throw 'PostgreSQL did not become healthy in time.' }

foreach ($service in $services) {
    Write-Host "Applying migrations with $service..."
    docker compose run --rm --no-deps $service --migrate-only
    if ($LASTEXITCODE -ne 0) { throw "Migration failed for $service." }
}

Write-Host 'All service migrations completed successfully.'
