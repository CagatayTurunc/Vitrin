# KAYIP EC2 BULMA REHBERİ
Write-Host "=== KAYIP EC2 INSTANCE ARAMA ===" -ForegroundColor Red

Write-Host @"
VİTRİN EC2'NİZ NEREDE? 🔍

1. 🌍 BAŞKA REGİON MU?
   - AWS Console'da sağ üstte region selector
   - Şu anda: eu-north-1 (Stockholm)
   - Diğer regionları kontrol edin:
     ✓ eu-west-1 (Ireland)
     ✓ eu-central-1 (Frankfurt) 
     ✓ us-east-1 (N. Virginia)
     ✓ us-west-2 (Oregon)

2. 💳 BAŞKA AWS HESABI MI?
   - Başka bir AWS hesabınız var mı?
   - Şirket hesabı vs kişisel hesap?
   - AWS Organizations multi-account?

3. ☁️ BAŞKA CLOUD PROVIDER MI?
   - Google Cloud Platform (GCP)
   - Microsoft Azure
   - DigitalOcean
   - Linode / Akamai
   - Vultr

4. 🔧 BAŞKA DEPLOYMENT METHOD MU?
   - Heroku
   - Vercel
   - Netlify
   - Railway
   - Render.com

"@ -ForegroundColor Yellow

Write-Host "=== HIZLI TESPİT YÖNTEMLERİ ===" -ForegroundColor Cyan
Write-Host @"
A) vitrin.it.com domain kayıtlarını kontrol edin:
   - whois vitrin.it.com
   - DNS records: A, CNAME kayıtları

B) Browser dev tools:
   - F12 → Network → vitrin.it.com'a gidin
   - Response headers'da server bilgileri

C) Terminal komutları:
   - nslookup vitrin.it.com
   - dig vitrin.it.com
   - curl -I https://vitrin.it.com

"@ -ForegroundColor White

Write-Host "=== HEMEN YAPIN ===" -ForegroundColor Green
Write-Host "1. AWS Console'da region değiştirin" -ForegroundColor Yellow
Write-Host "2. Farklı AWS hesaplarını kontrol edin" -ForegroundColor Yellow  
Write-Host "3. vitrin.it.com domain'in nereyi işaret ettiğine bakın" -ForegroundColor Yellow