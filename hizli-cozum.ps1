# HIZLI ÇÖZÜM - Artık beklemeyin!
Write-Host "=== HIZLI VITRIN DEPLOYMENT ===" -ForegroundColor Green

Write-Host "1. Docker Desktop başlayana kadar 30 saniye bekleyin..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

Write-Host "2. Docker durumunu kontrol ediyorum..." -ForegroundColor Cyan
try {
    docker --version
    Write-Host "✅ Docker çalışıyor!" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker henüz hazır değil, 15 saniye daha bekleyin" -ForegroundColor Red
    Start-Sleep -Seconds 15
}

Write-Host "3. Vitrin servislerini başlatıyorum..." -ForegroundColor Cyan
Set-Location "c:\Users\Cagatay\Desktop\Ürün Avcısı\Vitrin"

# Hızlı deployment
docker compose down --remove-orphans
docker compose -f docker-compose.yml -f docker-compose.prod.yml pull vitrin-web vitrin-gateway vitrin-auth
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d vitrin-web vitrin-gateway vitrin-auth

Write-Host "4. Servis durumunu kontrol ediyorum..." -ForegroundColor Cyan
docker compose ps

Write-Host "=== TAMAMLANDI! ===" -ForegroundColor Green
Write-Host "Artık sitez çalışır durumda olmalı!" -ForegroundColor Yellow