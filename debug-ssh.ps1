# SSH Bağlantı Debug Script
# GitHub Actions takıldığında manuel kontrol için

Write-Host "=== SSH Debug Script ===" -ForegroundColor Green

Write-Host @"
EC2 sunucunuza manuel SSH ile bağlanıp şunları kontrol edin:

# 1. SSH Bağlantısı Test
ssh -i YOUR_SSH_KEY ec2-user@YOUR_EC2_IP

# 2. Sistem durumu kontrol
uptime
df -h
free -h
docker ps
docker images

# 3. Vitrin klasörü kontrol
cd /home/ec2-user/vitrin
ls -la
pwd

# 4. Docker compose durumu
docker compose ps
docker compose logs --tail=20

# 5. Eğer takılıysa - hızlı restart
docker compose restart
docker compose ps

# 6. Ağ bağlantısı kontrol
ping google.com -c 3
curl -I github.com

# 7. Disk alanı kontrol
df -h
docker system df

# 8. GitHub Container Registry login test
echo "YOUR_GITHUB_TOKEN" | docker login ghcr.io -u YOUR_USERNAME --password-stdin
docker pull ghcr.io/cagatayturunce/vitrin-web:latest

"@ -ForegroundColor Yellow

Write-Host "`nBu komutları EC2'de çalıştırıp sonuçları paylaşın!" -ForegroundColor Cyan