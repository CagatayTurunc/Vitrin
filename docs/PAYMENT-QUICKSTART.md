# 💳 Payment Integration Quickstart — İyzico

> 15 dakikada çalışan bir ödeme sistemi!
> Hedef: MVP checkout flow

---

## 🎯 Adım 1: İyzico Sandbox Kurulumu (5 dakika)

### 1.1 Hesap Oluşturma
1. https://sandbox-merchant.iyzipay.com/auth/register adresine git
2. Email + şifre ile kayıt ol
3. Dashboard'a giriş yap

### 1.2 API Credentials Alma
```
Dashboard → Ayarlar → API Anahtarları

API Key: sandbox_xxxxxxxxxxxxxxxxxxxxxxxxxx
Secret Key: sandbox_yyyyyyyyyyyyyyyyyyyyyyyy
```

### 1.3 .env Dosyasına Ekle
```bash
# .env dosyasının sonuna ekle:

# İyzico Payment Gateway
IYZICO_API_KEY=sandbox_xxxxxxxxxxxxxxxxxxxxxxxxxx
IYZICO_SECRET_KEY=sandbox_yyyyyyyyyyyyyyyyyyyyyyyy
IYZICO_BASE_URL=https://sandbox-api.iyzipay.com
```

---

## 🎯 Adım 2: NuGet Package Ekle (2 dakika)

```bash
cd src/Services/Auth/Vitrin.Auth.Api
dotnet add package Iyzipay --version 2.1.39

cd ../../Infrastructure
dotnet add package Iyzipay --version 2.1.39
```

---

## 🎯 Adım 3: Payment Service Interface (5 dakika)

### 3.1 Shared Contracts
```bash
# Dosya oluştur
touch src/Shared/Vitrin.Shared.Contracts/Payment/IPaymentService.cs
```

Dosya içeriği aşağıda oluşturulacak.

---

## 🎯 Adım 4: Test Kartları (Referans)

İyzico sandbox test kartları:

| Kart Türü | Numara | CVV | Son Kullanma |
|-----------|--------|-----|--------------|
| Başarılı | 5528790000000008 | 123 | 12/30 |
| 3D Secure başarılı | 5528790000000008 | 123 | 12/30 |
| Başarısız (yetersiz bakiye) | 5406670000000009 | 123 | 12/30 |

**Kullanıcı bilgileri (test için):**
- Ad Soyad: John Doe
- Telefon: 0532 123 4567
- Adres: Test Mahallesi, Test Sokak No:1
- Şehir: İstanbul
- Ülke: Türkiye

---

## 🎯 Adım 5: Minimal Implementation

### 5.1 appsettings.json Güncelle

`src/Services/Auth/Vitrin.Auth.Api/appsettings.json`:
```json
{
  "Iyzico": {
    "ApiKey": "",  // .env'den gelecek
    "SecretKey": "",
    "BaseUrl": "https://sandbox-api.iyzipay.com"
  }
}
```

### 5.2 Docker appsettings güncelle

`src/Services/Auth/Vitrin.Auth.Api/appsettings.Docker.json`:
```json
{
  "Iyzico": {
    "ApiKey": "${IYZICO_API_KEY}",
    "SecretKey": "${IYZICO_SECRET_KEY}",
    "BaseUrl": "${IYZICO_BASE_URL}"
  }
}
```

---

## 🎯 Adım 6: Checkout Flow Test

### Frontend test (geçici):
```tsx
// Test butonu ekle
<button onClick={async () => {
  const response = await fetch('/api/v1/subscription/checkout', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ tier: 'ProMaker' })
  });
  
  const { checkoutUrl } = await response.json();
  window.location.href = checkoutUrl;
}}>
  Test Checkout
</button>
```

### Callback test:
```bash
# İyzico ödeme sonrası buraya yönlendirir:
# GET /checkout/success?token=xxx
# GET /checkout/failed?errorCode=xxx
```

---

## 🚀 Hızlı Deploy

```bash
# 1. .env dosyasını güncelle
vim .env

# 2. Docker rebuild
docker compose down
docker compose build auth
docker compose up -d auth

# 3. Health check
curl http://localhost:8080/health

# 4. Test checkout
# Frontend'den "Test Checkout" butonuna tıkla
```

---

## 📊 Debug — Sık Karşılaşılan Sorunlar

### Sorun 1: "Invalid signature" hatası
```
Çözüm: API Key ve Secret Key'i kontrol et
       Base64 encoding doğru mu?
```

### Sorun 2: Callback gelmiyor
```
Çözüm: CallbackUrl https olmalı (sandbox bile)
       ngrok kullan: ngrok http 5000
```

### Sorun 3: 3D Secure sayfası açılmıyor
```
Çözüm: Buyer bilgileri tam olmalı (ad, email, telefon, adres)
```

---

## ✅ Checklist

- [ ] İyzico sandbox hesabı oluşturuldu
- [ ] API credentials .env'e eklendi
- [ ] NuGet package yüklendi
- [ ] Payment service implement edildi
- [ ] Checkout endpoint eklendi
- [ ] Callback handler eklendi
- [ ] Test kartı ile ödeme yapıldı
- [ ] Subscription aktif oldu

---

## 🎯 Sonraki Adımlar

1. **Recurring payment** — Aylık otomatik ödeme
2. **Webhook handler** — İyzico event'lerini yakala
3. **Payment retry** — Başarısız ödemeleri tekrar dene
4. **Admin dashboard** — MRR, churn tracking

**Şimdilik MVP yeterli — yukarıdakiler production'a yaklaşırken eklenebilir.**
