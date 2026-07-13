# ==============================================================
# Vitrin - Smoke Test Script
# docker-compose up sonrası tüm servislerin sağlıklı olduğunu doğrular.
# Kullanım: .\scripts\smoke-test.ps1
# ==============================================================

$services = @(
    @{ Name = "Gateway";      Port = 5000;  Path = "/health" },
    @{ Name = "Frontend";     Port = 3000;  Path = "/" },
    @{ Name = "Auth API";     Port = 5104;  Path = "/health" },
    @{ Name = "Product API";  Port = 5177;  Path = "/health" },
    @{ Name = "Voting API";   Port = 5143;  Path = "/health" },
    @{ Name = "Comment API";  Port = 5100;  Path = "/health" },
    @{ Name = "Notif. API";   Port = 5101;  Path = "/health" },
    @{ Name = "Analytics API";Port = 5102;  Path = "/health" },
    @{ Name = "AI API";       Port = 5103;  Path = "/health" }
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
