# EC2 BULUNDU! 
Write-Host "=== VİTRİN EC2 BULUNDU! ===" -ForegroundColor Green

$EC2_IP = "63.180.14.214"
$EC2_DNS = "ec2-63-180-14-214.eu-central-1.compute.amazonaws.com"
$REGION = "eu-central-1"

Write-Host "Instance ID: 1eb6f90" -ForegroundColor Cyan
Write-Host "Region: $REGION (Frankfurt)" -ForegroundColor Cyan  
Write-Host "Public IP: $EC2_IP" -ForegroundColor Cyan
Write-Host "Public DNS: $EC2_DNS" -ForegroundColor Cyan
Write-Host "Status: Running ✅" -ForegroundColor Green

Write-Host "`n=== HEMEN YAPIN ===" -ForegroundColor Yellow
Write-Host @"
1. GitHub Secrets Kontrol:
   - Repository → Settings → Secrets and variables → Actions
   - EC2_HOST değeri şu mu: $EC2_IP veya $EC2_DNS

2. SSH Test:
   ssh -i your-key.pem ec2-user@$EC2_IP

3. Eğer GitHub Secrets yanlışsa güncelle:
   - EC2_HOST = $EC2_IP

4. Workflow yeniden çalıştır!

"@ -ForegroundColor White

Write-Host "=== HIZLI TEST ===" -ForegroundColor Red
Write-Host "Terminal'den test edin:" -ForegroundColor Yellow
Write-Host "nslookup vitrin.it.com" -ForegroundColor White
Write-Host "Bu IP'yi döndürüyor mu: $EC2_IP" -ForegroundColor White