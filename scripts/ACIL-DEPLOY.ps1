# ACİL DEPLOYMENT - GitHub Actions beğenmedik!
Write-Host "=== ACİL VİTRİN DEPLOYMENT ===" -ForegroundColor Red

# 1. GitHub Container Registry'den image çek
Write-Host "1. En son imageleri çekiyorum..." -ForegroundColor Yellow
docker pull ghcr.io/cagatayturunc/vitrin-web:latest
docker pull ghcr.io/cagatayturunc/vitrin-gateway:latest  
docker pull ghcr.io/cagatayturunc/vitrin-auth:latest

# 2. Mevcut servisleri durdur
Write-Host "2. Eski servisleri durdurup temizliyorum..." -ForegroundColor Yellow
docker stop vitrin-web vitrin-gateway vitrin-auth 2>$null
docker rm vitrin-web vitrin-gateway vitrin-auth 2>$null

# 3. Yeni servislerle başlat
Write-Host "3. Yeni servislerle başlatıyorum..." -ForegroundColor Yellow
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d vitrin-web vitrin-gateway vitrin-auth

# 4. Durumu kontrol et
Write-Host "4. Servis durumunu kontrol ediyorum..." -ForegroundColor Yellow
docker compose ps

Write-Host "=== DEPLOYMENT BİTTİ! ===" -ForegroundColor Green
Write-Host "Artık siteniz çalışır durumda!" -ForegroundColor Cyan