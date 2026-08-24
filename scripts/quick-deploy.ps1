# Hızlı manuel deployment scripti
# GitHub Actions beklemek yerine direkt EC2'ye deployment yapın

Write-Host "=== Manuel EC2 Deployment ===" -ForegroundColor Green

# SSH bağlantısı için gerekli bilgileri burada tanımlayın
$EC2_HOST = "YOUR_EC2_HOST"  # EC2 IP adresinizi buraya yazın
$EC2_USER = "ec2-user"       # SSH kullanıcı adınız
$SSH_KEY_PATH = "YOUR_SSH_KEY_PATH"  # SSH key dosyanızın yolu

Write-Host "EC2 Host: $EC2_HOST" -ForegroundColor Yellow
Write-Host "SSH ile bağlanmak için şu komutu kullanın:" -ForegroundColor Cyan

Write-Host @"

ssh -i $SSH_KEY_PATH $EC2_USER@$EC2_HOST

Bağlandıktan sonra şu komutları çalıştırın:

# 1. Vitrin klasörüne git
cd /home/ec2-user/vitrin

# 2. GitHub Container Registry'ye login ol
echo "YOUR_GITHUB_TOKEN" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin

# 3. Hızlı deployment (SSL kontrolü atlayarak)
docker compose -f docker-compose.yml -f docker-compose.prod.yml pull
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --remove-orphans

# 4. Servislerin durumunu kontrol et
docker compose ps

# 5. Logları kontrol et (sorun varsa)
docker compose logs vitrin-web
docker compose logs vitrin-gateway
docker compose logs vitrin-auth

"@ -ForegroundColor White

Write-Host "`n=== VEYA OTOMATIK SCRIPT ===" -ForegroundColor Green
Write-Host "EC2 bilgilerinizi yukarı kısma yazıp bu scripti çalıştırabilirsiniz:" -ForegroundColor Yellow