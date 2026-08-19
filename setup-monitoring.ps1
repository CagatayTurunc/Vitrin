# Vitrin Monitoring Stack Setup Script
# Run with PowerShell: .\setup-monitoring.ps1

Write-Host "Starting Vitrin Monitoring Stack..." -ForegroundColor Green

# 1. Start Docker services
Write-Host "Starting Docker services..." -ForegroundColor Yellow
docker-compose -f docker-compose.monitoring.yml up -d

# 2. Wait for services to start
Write-Host "Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# 3. Check Prometheus health
Write-Host "Checking Prometheus..." -ForegroundColor Yellow
try {
    $prometheusResponse = Invoke-RestMethod -Uri "http://localhost:9090/-/healthy" -TimeoutSec 10
    Write-Host "Prometheus is running successfully!" -ForegroundColor Green
} catch {
    Write-Host "Cannot connect to Prometheus!" -ForegroundColor Red
}

# 4. Check Grafana health
Write-Host "Checking Grafana..." -ForegroundColor Yellow
try {
    $grafanaResponse = Invoke-RestMethod -Uri "http://localhost:3000/api/health" -TimeoutSec 10
    Write-Host "Grafana is running successfully!" -ForegroundColor Green
} catch {
    Write-Host "Cannot connect to Grafana!" -ForegroundColor Red
}

# 5. Dashboard import instructions
Write-Host ""
Write-Host "SETUP COMPLETED!" -ForegroundColor Green
Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Cyan
Write-Host "1. Open http://localhost:3000 in your browser" -ForegroundColor White
Write-Host "2. Login - Username: admin, Password: admin123" -ForegroundColor White
Write-Host "3. Click '+' -> 'Import' from left menu" -ForegroundColor White
Write-Host "4. Upload 'vitrin-production-dashboard-v2.json' file" -ForegroundColor White
Write-Host ""
Write-Host "SERVICE URLS:" -ForegroundColor Cyan
Write-Host "Grafana: http://localhost:3000" -ForegroundColor White
Write-Host "Prometheus: http://localhost:9090" -ForegroundColor White
Write-Host "AlertManager: http://localhost:9093" -ForegroundColor White
Write-Host ""
Write-Host "METRICS ENDPOINTS:" -ForegroundColor Cyan
Write-Host "Vitrin App: http://localhost:5000/metrics" -ForegroundColor White
Write-Host "PostgreSQL: http://localhost:9187/metrics" -ForegroundColor White
Write-Host "Redis: http://localhost:9121/metrics" -ForegroundColor White
Write-Host ""
Write-Host "Read README.md file for dashboard import instructions!" -ForegroundColor Yellow