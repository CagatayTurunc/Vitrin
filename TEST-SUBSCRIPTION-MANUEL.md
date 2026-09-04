# 🧪 Vitrin Subscription & Payment Sistemi - Manuel Test Rehberi

## 📋 Test Öncesi Hazırlık

### 1. Servisleri Başlat
```powershell
cd "c:\Users\Cagatay\Desktop\Ürün Avcısı\Vitrin"
.\START-ALL-SERVICES.ps1
```

**Bekle:** ~60 saniye (tüm servislerin başlaması için)

### 2. Tarayıcıyı Aç
- Chrome veya Edge'i aç
- F12 ile Developer Tools'u aç (Network ve Console sekmelerini kullanacağız)

---

## 🧪 TEST SENARYOLARI

### ✅ **Test 1: Kullanıcı Kaydı ve Free Tier**

#### Adımlar:
1. **`http://localhost:3000`** adresine git
2. **"Kayıt Ol"** butonuna tıkla
3. Formu doldur:
   ```
   E-posta  : test@vitrin.local
   Kullanıcı: testuser123
   Ad Soyad : Test User
   Şifre    : Test1234!
   ```
4. **Kayıt Ol** butonuna tıkla

#### Beklenen Sonuç:
- ✅ Başarı mesajı görünmeli
- ✅ Otomatik giriş yapılmalı
- ✅ Dashboard'a yönlendirilmeli
- ✅ Sağ üstte **"Free"** badge'i görünmeli

#### Debug:
- Developer Console'da hata var mı kontrol et
- Network sekmesinde `/api/auth/register` isteğinin **200 OK** dönmesini kontrol et

---

### ✅ **Test 2: Abonelik Durumu Kontrolü**

#### Adımlar:
1. **`http://localhost:3000/dashboard`** adresine git
2. Sol menüden **"Abonelik"** sekmesine tıkla

#### Beklenen Sonuç:
- ✅ **Tier:** Free
- ✅ **Status:** Active
- ✅ **Max Ürün:** 5
- ✅ **"Pro'ya Geç"** butonu görünmeli

#### API Testi (Opsiyonel - Postman/Thunder Client):
```
GET http://localhost:5000/api/subscription/me
Authorization: Bearer {JWT_TOKEN}
```

---

### ✅ **Test 3: Pricing Sayfası**

#### Adımlar:
1. **`http://localhost:3000/pricing`** adresine git
2. Sayfada 3 tier görünmeli:
   - **Free** (₺0/ay)
   - **Pro Maker** (₺99/ay)
   - **Enterprise** (₺299/ay)

#### Beklenen Sonuç:
- ✅ Her tier'ın özellikleri listelenmiş olmalı
- ✅ "Şimdi Başla" butonları çalışmalı
- ✅ Fiyatlar TL olarak gösterilmeli

---

### ✅ **Test 4: Checkout (İyzico Sandbox)**

#### Adımlar:
1. **Pricing sayfasında** → **Pro Maker** → **"Şimdi Başla"**
2. Kupon kodu alanı görünmeli (boş bırak)
3. **"Ödemeye Geç"** butonuna tıkla

#### Beklenen Sonuç:
- ✅ İyzico sandbox ödeme sayfası açılmalı (yeni sekme)
- ✅ **Tutar:** ₺99.00
- ✅ Ödeme formu görünmeli

#### İyzico Test Kartı:
```
Kart No   : 5528 7900 0000 0008
SKT       : 12/30
CVC       : 123
Kart Sahibi: TEST USER
3D Şifre  : a
```

#### Adımlar (İyzico Sayfasında):
1. Test kartı bilgilerini gir
2. **"Ödeme Yap"** → 3D Secure sayfası açılır
3. Şifre: **`a`** → Onayla

#### Beklenen Sonuç:
- ✅ Ödeme başarılı mesajı
- ✅ Vitrin'e geri yönlendirme (`/welcome?success=true`)
- ✅ Dashboard'da **"Pro Maker"** badge'i görünmeli

---

### ✅ **Test 5: Kupon Kodu Uygulama**

#### Hazırlık (Admin - Kupon Oluştur):
**Postman/Thunder Client ile:**
```
POST http://localhost:5000/api/discount/admin/create
Authorization: Bearer {ADMIN_JWT_TOKEN}
Content-Type: application/json

{
  "code": "LAUNCH50",
  "description": "İlk 100 kullanıcıya %50 indirim",
  "type": 0,
  "value": 50,
  "applicableTiers": [1, 2],
  "maxUses": 100,
  "maxUsesPerUser": 1,
  "expiresAt": "2026-12-31T23:59:59Z"
}
```

**NOT:** Admin token almak için önce bir kullanıcıyı manuel olarak database'den Admin yapmalısın:
```sql
UPDATE "Users" SET "Role" = 2 WHERE "Email" = 'test@vitrin.local';
```

#### Test Adımları:
1. **Pricing** → **Pro Maker** → **"Şimdi Başla"**
2. Kupon kodu alanına: **`LAUNCH50`** yaz
3. **"Uygula"** butonuna tıkla

#### Beklenen Sonuç:
- ✅ Başarı mesajı: "Kupon uygulandı!"
- ✅ Fiyat: ~~₺99~~ → **₺49.50** (50% indirim)
- ✅ Ödemeye geçince İyzico'da **₺49.50** görünmeli

---

### ✅ **Test 6: Fatura PDF İndirme**

#### Adımlar:
1. Dashboard → **Abonelik** → **Ödeme Geçmişi**
2. Son ödemeyi bul
3. **"Fatura İndir"** butonuna tıkla

#### Beklenen Sonuç:
- ✅ PDF dosyası indirilmeli
- ✅ Fatura bilgileri: Tarih, Tutar, Kupon (varsa), Vergi

#### API Testi:
```
GET http://localhost:5000/api/subscription/invoices/{INVOICE_ID}/pdf
Authorization: Bearer {JWT_TOKEN}
```

---

### ✅ **Test 7: Abonelik İptali**

#### Adımlar:
1. Dashboard → **Abonelik**
2. **"Aboneliği İptal Et"** butonuna tıkla
3. İptal nedenini seç (opsiyonel)
4. **"Onayla"** butonuna tıkla

#### Beklenen Sonuç:
- ✅ Uyarı mesajı: "Aboneliğiniz dönem sonunda (XX Tarih) iptal edilecek"
- ✅ **Status:** "Active" kalmalı
- ✅ **"CancelAtPeriodEnd":** true
- ✅ **"Aboneliği Tekrar Aktifleştir"** butonu görünmeli

#### Kontrol:
```
GET http://localhost:5000/api/subscription/me
```
Response:
```json
{
  "cancelAtPeriodEnd": true,
  "currentPeriodEnd": "2026-09-30T..."
}
```

---

### ✅ **Test 8: Reactivation (İptalden Vazgeç)**

#### Adımlar:
1. İptal edilmiş abonelik ekranında
2. **"Aboneliği Tekrar Aktifleştir"** butonuna tıkla

#### Beklenen Sonuç:
- ✅ Başarı mesajı
- ✅ **"CancelAtPeriodEnd":** false
- ✅ Abonelik dönem sonunda yenilenecek

---

### ✅ **Test 9: Quota Enforcement (5 Ürün Limiti)**

#### Adımlar:
1. Free tier kullanıcı olarak giriş yap
2. Dashboard → **Ürünlerim** → **"Yeni Ürün"**
3. **5 ürün oluştur** (hızlıca)
4. **6. ürünü** oluşturmaya çalış

#### Beklenen Sonuç:
- ✅ Hata mesajı: "Free tier'da maksimum 5 ürün oluşturabilirsiniz"
- ✅ HTTP 403 Forbidden
- ✅ **"Pro'ya Geç"** önerisi görünmeli

---

### ✅ **Test 10: Admin Dashboard**

#### Adımlar:
1. Admin kullanıcı olarak giriş yap
2. **`http://localhost:3000/admin/subscriptions`** adresine git

#### Beklenen Sonuç:
- ✅ **MRR (Monthly Recurring Revenue)** gösterilmeli
- ✅ **Toplam Aktif Abonelik** sayısı
- ✅ **Tier Dağılımı** (Free, Pro, Enterprise)
- ✅ **Son Ödemeler** listesi

#### API Testi:
```
GET http://localhost:5000/api/subscription/admin/stats
Authorization: Bearer {ADMIN_JWT_TOKEN}
```

Response:
```json
{
  "mrr": 99.00,
  "totalActive": 1,
  "totalPro": 1,
  "totalEnterprise": 0,
  "totalFree": 5
}
```

---

## 🔧 Sorun Giderme

### Servisler Çalışmıyor?
```powershell
# Logları kontrol et
docker logs vitrin-postgres
docker logs vitrin-redis
docker logs vitrin-kafka

# Auth service logs
cd src/Services/Auth/Vitrin.Auth.Api
dotnet run --verbosity detailed
```

### JWT Token Al (Manuel Test İçin):
```powershell
# Login
$body = @{ email="test@vitrin.local"; password="Test1234!" } | ConvertTo-Json
$response = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" -Method POST -Body $body -ContentType "application/json"
$token = $response.token
Write-Host "Token: $token"
```

### Database'de Kullanıcıyı Admin Yap:
```powershell
docker exec -it vitrin-postgres psql -U postgres -d vitrin_auth -c "UPDATE \"Users\" SET \"Role\" = 2 WHERE \"Email\" = 'test@vitrin.local';"
```

---

## ✅ Başarı Kriterleri

Tüm testler başarılı olmalı:
- ✅ Kayıt ve giriş çalışıyor
- ✅ Free → Pro geçiş İyzico ile yapılıyor
- ✅ Kupon kodu indirim uyguluyor
- ✅ Fatura PDF oluşuyor
- ✅ İptal ve reactivation çalışıyor
- ✅ Quota enforcement aktif
- ✅ Admin dashboard MRR gösteriyor

---

## 🎉 Test Tamamlandı!

Herhangi bir sorunla karşılaşırsan:
1. Browser Console'u kontrol et
2. Network sekmesinde API yanıtlarına bak
3. Backend loglarını incele
