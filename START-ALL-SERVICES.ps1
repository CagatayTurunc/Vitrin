# ============================================================
# Vitrin - Tüm Servisleri Başlat
# ============================================================

Write-Host "🚀 Vitrin servisleri baslatiliyor..." -ForegroundColor Cyan

# Docker servisleri baslat
Write-Host "`n1️⃣  Docker servisleri baslatiliyor (PostgreSQL, Redis, Kafka)..." -ForegroundColor Yellow
docker compose up -d postgres redis kafka

Write-Host "`n⏳ Veritabanlarin hazir olmasi bekleniyor (30 saniye)..." -ForegroundColor Gray
Start-Sleep -Seconds 30

# Auth Service
Write-Host "`n2️⃣  Auth Service baslatiliyor (Port 5104)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\src\Services\Auth\Vitrin.Auth.Api'; dotnet run"

Start-Sleep -Seconds 5

# Gateway
Write-Host "`n3️⃣  API Gateway baslatiliyor (Port 5000)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\src\Gateways\Vitrin.Gateway'; dotnet run"

Start-Sleep -Seconds 5

# Frontend
Write-Host "`n4️⃣  Next.js Frontend baslatiliyor (Port 3000)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\src\Web\Vitrin.Web.UI'; npm run dev"

Write-Host "`n✅ Tum servisler baslatildi!" -ForegroundColor Green
Write-Host "`n📋 Servis Adresleri:" -ForegroundColor Cyan
Write-Host "   Frontend  : http://localhost:3000" -ForegroundColor White
Write-Host "   Gateway   : http://localhost:5000" -ForegroundColor White
Write-Host "   Auth API  : http://localhost:5104" -ForegroundColor White
Write-Host "`n⏰ Servislerin tamamen hazir olmasi icin 20 saniye daha bekleyin..." -ForegroundColor Gray
