# EC2 BAĞLANTI SORUNU ÇÖZÜMÜ
Write-Host "=== EC2 BAĞLANTI SORUNU ÇÖZÜMÜ ===" -ForegroundColor Red

Write-Host @"
SSH TIMEOUT SORUNU: dial tcp ***:22: i/o timeout

HEMEN KONTROL EDİN:

1. 🔍 AWS KONSOL KONTROLÜ:
   - AWS Console → EC2 → Instances
   - EC2 instance durumu: Running mi? Stopped mu?
   - Eğer STOPPED ise: Start Instance butonuna tıklayın!

2. 🔧 ELASTİC IP KONTROLÜ:
   - EC2 instance'ının IP adresi değişmiş olabilir
   - GitHub Secrets'daki EC2_HOST güncel mi?
   - Elastic IP atanmış mı, yoksa public IP mi kullanıyorsunuz?

3. 🛡️ GÜVENLİK GRUBU KONTROLÜ:
   - Security Groups → SSH (Port 22) açık mı?
   - 0.0.0.0/0 dan SSH erişimine izin var mı?

4. 🔑 SSH KEY KONTROLÜ:
   - GitHub Secrets'daki EC2_SSH_KEY doğru mu?
   - SSH key formatı doğru mu?

5. 🌐 NETWORK KONTROLÜ:
   - VPC/Subnet ayarları değişti mi?
   - Internet Gateway bağlantısı var mı?

HIZLI TEST:
Local terminalden şu komutu çalıştırın:
ssh -i YOUR_SSH_KEY ec2-user@YOUR_EC2_IP

Eğer bağlanmıyorsa EC2 kesinlikle kapalı/erişilemez!

"@ -ForegroundColor Yellow

Write-Host "=== ALTERNATİF ÇÖZÜMLER ===" -ForegroundColor Cyan
Write-Host "1. EC2'yi restart edin" -ForegroundColor White
Write-Host "2. Yeni bir EC2 instance oluşturun" -ForegroundColor White  
Write-Host "3. CloudFlare/Vercel gibi alternatif deployment" -ForegroundColor White
Write-Host "4. Docker Hub + webhooks kullanın" -ForegroundColor White