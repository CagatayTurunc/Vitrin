---
inclusion: manual
---

# Vitrin QA Agent — Talimatlar

Kullanıcı "test et", "siteyi kontrol et", "butonları test et" veya benzeri bir şey dediğinde bu talimatları uygula.

## Site Bilgileri

- **Production:** https://vitrin.it.com
- **Local dev:** http://localhost:3001
- **API Gateway:** http://localhost:5000 (local) / https://vitrin.it.com (prod)

## Test Akışı

Aşağıdaki sırayla ilerle. Her adımda **screenshot al**, sonuçları not et.

---

### 1. Genel Sayfa Sağlığı

Her sayfayı ziyaret et, yüklenip yüklenmediğini kontrol et:

- `/` Ana sayfa
- `/login`
- `/register`
- `/forgot-password`
- `/launches`
- `/launches/upcoming`
- `/categories`
- `/search`
- `/submit`
- `/compare`
- `/about`

Her sayfa için kontrol et:
- Sayfa 200 ile mi yüklendi?
- Console'da JS hatası var mı? (`browser_console_messages`)
- Sayfa boş mu, içerik var mı?
- Network'te 5xx hata var mı? (`browser_network_requests`)

---

### 2. Login Formu

`/login` sayfasına git:
- E-posta alanı görünüyor mu?
- Şifre alanı görünüyor mu?
- "Giriş Yap" butonu aktif mi?
- Google / GitHub butonları var mı?
- Yanlış kimlikle giriş dene: `yanlis@test.com` / `Yanlis123!`
- Hata mesajı gösteriliyor mu?
- API isteği `/api/auth/login`'e gitti mi? Hangi HTTP kodu döndü?

---

### 3. Kayıt Formu

`/register` sayfasına git:
- Tüm alanlar (ad soyad, kullanıcı adı, e-posta, şifre) görünüyor mu?
- "Kayıt Ol" butonu aktif mi?
- Form göndermeden butona tıkla — validation çalışıyor mu?

---

### 4. Maker Ol Akışı ← KRİTİK

`/submit` sayfasına git:
- Sayfa ne gösteriyor? "Maker ol" mu, "Yeni ürün ekle" mi, yoksa login'e mi yönlendiriyor?
- Eğer "Maker ol" formu varsa: portfolyo URL ve neden alanları görünüyor mu?
- "Başvuruyu gönder" butonu var mı, tıklanabilir mi?
- API isteği `/api/auth/maker-applications`'a gidiyor mu?

---

### 5. Ana Sayfa Ürün Listesi

`/` sayfasına dön:
- Ürün kartları yüklendi mi?
- Upvote (oy ver) butonu görünüyor mu?
- Upvote butonuna tıkla — ne oluyor? Login sayfasına mı yönlendiriyor, API isteği mi gönderiyor?
- API isteği `/api/products` başarılı mı döndü?

---

### 6. Arama

`/search` sayfasına git:
- Arama kutusu var mı?
- "uygulama" yaz ve ara
- Sonuçlar geliyor mu?
- API isteği `/api/products/search` veya `/api/products`'a gitti mi?

---

### 7. Kategoriler

`/categories` sayfasına git:
- Kategori listesi yüklendi mi?
- API isteği `/api/categories`'e gitti mi, 200 döndü mü?

---

### 8. Network & API Özeti

Tüm sayfalarda topladığın network isteklerini listele:
- Hangi endpoint'ler 2xx döndü ✅
- Hangi endpoint'ler 4xx döndü ⚠️ (beklenebilir — auth gerekiyor)
- Hangi endpoint'ler 5xx döndü ❌ (servis sorunu)
- Hiç istek gitmeyen ama gitmesi gereken endpoint'ler ❌

---

## Rapor Formatı

Test bittikten sonra şu formatta rapor ver:

```
## Vitrin QA Raporu — [Tarih]
**Test Edilen URL:** https://vitrin.it.com

### 🏥 Genel Durum: [İYİ / ORTA / KRİTİK]

### ✅ Çalışanlar
- ...

### ❌ Sorunlar
- [Sayfa/Buton/Özellik]: [Ne oldu] → [API endpoint varsa hangisi, hangi HTTP kodu]

### ⚠️ UX / Görsel Sorunlar  ← AI değerlendirmesi
- Screenshot'lara bakarak: buton çok küçük mü, sayfa bozuk mu, akış kafa karıştırıcı mı, eksik bir şey var mı?

### 💡 Öneriler
- ...
```

---

## Önemli Notlar

- Her kritik adımdan sonra `browser_screenshot` al
- Console hatalarını `browser_console_messages` ile topla
- Network isteklerini `browser_network_requests` ile kontrol et
- Bir şey çalışmıyorsa tahmin etme — ekran görüntüsünde ne gördüğünü söyle
- UX sorunları için: "bu buton çok küçük", "bu hata mesajı kullanıcıya belirsiz geliyor", "bu akış gereksiz karmaşık" gibi gözlemler de ekle
