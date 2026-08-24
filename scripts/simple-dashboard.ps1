Write-Host "Grafana Dashboard Creator" -ForegroundColor Green

$grafanaUrl = "http://localhost:3004"

Write-Host "Grafana admin password girin:"
$password = Read-Host

$auth = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("admin:$password"))
$headers = @{
    'Content-Type' = 'application/json'
    'Authorization' = "Basic $auth"
}

try {
    Write-Host "Testing connection..." -ForegroundColor Yellow
    $orgResponse = Invoke-RestMethod -Uri "$grafanaUrl/api/org" -Headers $headers
    Write-Host "Connection successful! Org: $($orgResponse.name)" -ForegroundColor Green
} catch {
    Write-Host "Cannot connect to Grafana: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Try opening http://localhost:3004 in browser first" -ForegroundColor Yellow
    exit 1
}

Write-Host "Opening Grafana in browser..." -ForegroundColor Green
Start-Process $grafanaUrl

Write-Host ""
Write-Host "MANUAL STEPS:" -ForegroundColor Cyan
Write-Host "1. Click '+' -> Dashboard" -ForegroundColor White
Write-Host "2. Add panel with query: up{job=~'vitrin.*'}" -ForegroundColor White
Write-Host "3. Set panel type to 'Stat'" -ForegroundColor White
Write-Host "4. Add more panels with these queries:" -ForegroundColor White
Write-Host "   - CPU: rate(process_cpu_seconds_total{job=~'vitrin.*'}[5m]) * 100" -ForegroundColor White
Write-Host "   - Memory: process_resident_memory_bytes{job=~'vitrin.*'}" -ForegroundColor White
Write-Host "   - Redis: rate(redis_commands_processed_total[5m])" -ForegroundColor White
Write-Host ""
Write-Host "Done!" -ForegroundColor Green