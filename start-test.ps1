Write-Host "Starting Vitrin services..." -ForegroundColor Cyan

Write-Host "Starting Docker services..." -ForegroundColor Yellow
docker compose up -d postgres redis kafka

Write-Host "Waiting for databases (30 sec)..." -ForegroundColor Gray
Start-Sleep -Seconds 30

Write-Host "Starting Auth Service (Port 5104)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\src\Services\Auth\Vitrin.Auth.Api'; dotnet run"

Start-Sleep -Seconds 5

Write-Host "Starting Gateway (Port 5000)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\src\Gateways\Vitrin.Gateway'; dotnet run"

Start-Sleep -Seconds 5

Write-Host "Starting Frontend (Port 3000)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\src\Web\Vitrin.Web.UI'; npm run dev"

Write-Host ""
Write-Host "All services started!" -ForegroundColor Green
Write-Host ""
Write-Host "Service URLs:" -ForegroundColor Cyan
Write-Host "  Frontend : http://localhost:3000" -ForegroundColor White
Write-Host "  Gateway  : http://localhost:5000" -ForegroundColor White
Write-Host "  Auth API : http://localhost:5104" -ForegroundColor White
Write-Host ""
Write-Host "Wait 20 more seconds for services to be ready..." -ForegroundColor Gray
