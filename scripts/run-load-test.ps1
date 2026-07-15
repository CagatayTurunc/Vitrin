param(
    [string]$BaseUrl = "http://host.docker.internal:5000",
    [int]$VirtualUsers = 5,
    [string]$Duration = "15s"
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

docker run --rm `
    --env "BASE_URL=$BaseUrl" `
    --env "VUS=$VirtualUsers" `
    --env "DURATION=$Duration" `
    --volume "${root}:/workspace:ro" `
    grafana/k6:2.0.0 run /workspace/tests/load/products-smoke.js

if ($LASTEXITCODE -ne 0) {
    throw "k6 load testi başarısız oldu (çıkış kodu: $LASTEXITCODE)."
}
