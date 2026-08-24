# SSH Key Dosyası Oluşturma
Write-Host "=== SSH KEY DOSYASI OLUŞTURMA ===" -ForegroundColor Green

Write-Host @"
1. GitHub'dan SSH Key alın:
   - Repository → Settings → Secrets and variables → Actions
   - EC2_SSH_KEY secret'ını kopyalayın

2. SSH Key dosyası oluşturun:
   - Notepad açın
   - GitHub'dan kopyaladığınız key'i yapıştırın
   - -----BEGIN RSA PRIVATE KEY----- ile başlamalı
   - -----END RSA PRIVATE KEY----- ile bitmeli
   - Dosyayı şu path'e kaydedin: C:\Users\Cagatay\.ssh\vitrin-key.pem

3. Key permissions (Windows):
   - Dosyaya sağ tık → Properties → Security
   - Advanced → Change Permissions
   - Sadece owner'a full control verin

4. SSH Test komutu:
   ssh -i C:\Users\Cagatay\.ssh\vitrin-key.pem ec2-user@63.180.14.214

"@ -ForegroundColor Yellow

Write-Host "`n=== ALTERNATİF: PUTTY KULLANIN ===" -ForegroundColor Cyan
Write-Host @"
Eğer SSH çalışmazsa:
1. PuTTY indirin: https://putty.org
2. Host: 63.180.14.214
3. Port: 22
4. Connection → SSH → Auth → Browse → .pem dosyasını seçin
5. Open → ec2-user ile login

"@ -ForegroundColor White

Write-Host "=== HIZLI KONTROL ===" -ForegroundColor Red
Write-Host "Önce ping test:" -ForegroundColor Yellow
Write-Host "ping 63.180.14.214" -ForegroundColor White