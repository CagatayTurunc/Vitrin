# ==============================================================
# Vitrin - Smoke Test Script
# Docker Compose'un dışarı açtığı giriş noktalarını doğrular.
# İç servisler Compose healthcheck'leriyle Gateway arkasında denetlenir.
# Kullanım: .\scripts\smoke-test.ps1
# ==============================================================

$services = @(
    @{ Name = "Gateway";      Port = 5000;  Path = "/health" },
    @{ Name = "Frontend";     Port = 3000;  Path = "/" }
)

$passed = 0
$failed = 0

Write-Host ""
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  Vitrin Smoke Test" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host ""

foreach ($svc in $services) {
    $url = "http://localhost:$($svc.Port)$($svc.Path)"
    try {
        $response = Invoke-WebRequest -Uri $url -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host "  [OK] $($svc.Name.PadRight(15)) $url" -ForegroundColor Green
            $passed++
        } else {
            Write-Host "  [WARN] $($svc.Name.PadRight(15)) $url  (HTTP $($response.StatusCode))" -ForegroundColor Yellow
            $failed++
        }
    } catch {
        Write-Host "  [FAIL] $($svc.Name.PadRight(15)) $url  -> $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }
}

Write-Host ""
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  Sonuç: $passed geçti, $failed başarısız" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host ""

if ($failed -gt 0) {
    exit 1
}
