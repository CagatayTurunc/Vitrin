# Production Environment Fix

## Sorun
E-posta linkleri hala IP adresi kullanıyor: `63.180.14.214:3002`
Domain kullanması gerekiyor: `https://vitrin.it.com`

## Çözüm
Production sunucusundaki .env dosyasında bu değişkeni güncellemek:

```bash
# Eski (yanlış)
EMAIL_APP_BASE_URL=http://63.180.14.214:3002

# Yeni (doğru) 
EMAIL_APP_BASE_URL=https://vitrin.it.com
```

## Manuel Fix Komutları

SSH ile production sunucusuna bağlanıp:

```bash
cd /home/ec2-user/vitrin

# .env dosyasını düzenle
sed -i 's|EMAIL_APP_BASE_URL=.*|EMAIL_APP_BASE_URL=https://vitrin.it.com|g' .env

# Auth servisini restart et (e-posta konfigürasyonunu yeniden yükler)
docker restart vitrin-auth

# Log'ları kontrol et
docker logs vitrin-auth --tail 20
```

## Kontrol
Restart sonrası yeni kayıt yaparsan e-posta linki artık:
`https://vitrin.it.com/confirm-email?token=...` olacak