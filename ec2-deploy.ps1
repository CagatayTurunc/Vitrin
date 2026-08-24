# EC2 Production Deployment Script
# vitrin.it.com için production deployment

Write-Host "=== EC2 PRODUCTION DEPLOYMENT ===" -ForegroundColor Green

# EC2 bilgileri (GitHub Actions secrets'den alın)
Write-Host @"
SSH ile EC2'ye bağlanın:

ssh -i YOUR_SSH_KEY ec2-user@YOUR_EC2_IP

Sonra şu komutları çalıştırın:

# 1. Vitrin dizinine git
cd /home/ec2-user/vitrin

# 2. GitHub Container Registry'ye login
echo "YOUR_GITHUB_TOKEN" | docker login ghcr.io -u YOUR_USERNAME --password-stdin

# 3. En son imageleri çek
docker compose -f docker-compose.yml -f docker-compose.prod.yml pull vitrin-web vitrin-gateway vitrin-auth

# 4. Servisleri yeniden başlat
docker compose -f docker-compose.yml -f docker-compose.prod.yml stop vitrin-web vitrin-gateway vitrin-auth
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d vitrin-web vitrin-gateway vitrin-auth

# 5. Durumu kontrol et
docker compose ps
docker logs vitrin-web --tail 10
docker logs vitrin-gateway --tail 10

# 6. Site testi
curl -I https://vitrin.it.com

"@ -ForegroundColor Yellow

Write-Host "`n=== HIZLI ÇÖZÜM ===" -ForegroundColor Cyan
Write-Host "EC2'de Kafka yoksa şu komutları çalıştırın:" -ForegroundColor Yellow
Write-Host "docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d kafka zookeeper" -ForegroundColor White
Write-Host "docker restart vitrin-auth vitrin-gateway vitrin-web" -ForegroundColor White