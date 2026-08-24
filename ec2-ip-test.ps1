# EC2 Instance IP Test
Write-Host "=== EC2 INSTANCE IP TESİ ===" -ForegroundColor Green

Write-Host @"
AWS Console'dan IP'leri alın:

1. 'gotur-producti...' instance'ına tıklayın
   - Public IPv4 address: _______________
   - Public IPv4 DNS: _______________

2. 'test' instance'ına tıklayın  
   - Public IPv4 address: _______________
   - Public IPv4 DNS: _______________

3. GitHub Secrets kontrol:
   - Repository → Settings → Secrets → EC2_HOST değeri: _______________

4. SSH Test komutları:
   ssh -i your-key.pem ec2-user@IP1
   ssh -i your-key.pem ec2-user@IP2

5. Vitrin dizini kontrolü:
   ls -la /home/ec2-user/
   ls -la /home/ec2-user/vitrin/

Hangi instance'da 'vitrin' klasörü varsa o doğru instance!

"@ -ForegroundColor Yellow

Write-Host "=== HIZLI ÇÖZÜM ===" -ForegroundColor Cyan
Write-Host "Eğer hiç birinde vitrin yoksa:" -ForegroundColor White
Write-Host "1. Yeni EC2 instance oluşturun" -ForegroundColor White
Write-Host "2. Docker + Docker Compose kurun" -ForegroundColor White
Write-Host "3. GitHub'dan code'u çekin" -ForegroundColor White
Write-Host "4. GitHub Secrets'ı güncelleyin" -ForegroundColor White