# ============================================================
# Vitrin Smoke Test Çalıştırıcı
#
# Kullanım:
#   .\run-smoke-tests.ps1                          → local (localhost:3001)
#   .\run-smoke-tests.ps1 -Env production          → production (vitrin.it.com)
#   .\run-smoke-tests.ps1 -Suite health            → sadece health check
#   .\run-smoke-tests.ps1 -Suite auth              → authenticated akışlar
#   .\run-smoke-tests.ps1 -Open                    → test bitti, raporu aç
# ============================================================

param(
    [ValidateSet("local", "production")]
    [string]$Env = "local",

    [ValidateSet("smoke", "health", "auth", "all")]
    [string]$Suite = "smoke",

    [switch]$Open
)

$ErrorActionPreference = "Stop"
$uiDir = Join-Path $PSScriptRoot "src\Web\Vitrin.Web.UI"

# URL seç
$baseUrl = if ($Env -eq "production") { "https://vitrin.it.com" } else { "http://localhost:3001" }
$gatewayUrl = if ($Env -eq "production") { "https://vitrin.it.com" } else { "http://localhost:5000" }

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Vitrin E2E Smoke Test" -ForegroundColor Cyan
Write-Host "  Ortam  : $Env ($baseUrl)" -ForegroundColor DarkGray
Write-Host "  Suite  : $Suite" -ForegroundColor DarkGray
Write-Host "======================================" -ForegroundColor Cyan

# Env değişkenlerini ayarla
$env:PLAYWRIGHT_BASE_URL = $baseUrl
$env:GATEWAY_URL         = $gatewayUrl

# .env dosyasından test kimlik bilgilerini oku (varsa)
$envPath = Join-Path $PSScriptRoot ".env"
if (Test-Path $envPath) {
    foreach ($line in Get-Content $envPath) {
        if ($line -match "^E2E_TEST_EMAIL=(.+)$")    { $env:E2E_TEST_EMAIL    = $Matches[1].Trim() }
        if ($line -match "^E2E_TEST_PASSWORD=(.+)$") { $env:E2E_TEST_PASSWORD = $Matches[1].Trim() }
    }
}

# pnpm komutunu seç
$grepFlag = switch ($Suite) {
    "smoke"  { "--grep @smoke" }
    "health" { "--grep @health" }
    "auth"   { "--grep @auth-flow" }
    "all"    { "" }
}

$cmd = "pnpm test:e2e $grepFlag".Trim()

Write-Host ""
Write-Host "Çalıştırılıyor: $cmd" -ForegroundColor Yellow
Write-Host ""

Push-Location $uiDir
try {
    Invoke-Expression $cmd
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

# Rapor
$reportPath = Join-Path $PSScriptRoot "artifacts\playwright-report\index.html"
if (Test-Path $reportPath) {
    Write-Host ""
    Write-Host "Rapor: $reportPath" -ForegroundColor Green

    if ($Open -or $exitCode -ne 0) {
        Start-Process $reportPath
    }
}

if ($exitCode -ne 0) {
    Write-Host ""
    Write-Host "❌ Bazı testler başarısız oldu. Raporu incele." -ForegroundColor Red
    exit $exitCode
} else {
    Write-Host ""
    Write-Host "✅ Tüm testler geçti." -ForegroundColor Green
}
