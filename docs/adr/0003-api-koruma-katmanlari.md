# ADR-0003: API hata, kota, rate limit ve audit katmanları

- Durum: Kabul edildi
- Tarih: 2026-07-14
- Karar sahipleri: Vitrin geliştirme ekibi

## Bağlam

Servisler iş kuralı hatalarını farklı JSON şekillerinde döndürüyor, beklenmeyen hatalar için ortak bir güvenli yanıt üretmiyor ve istemciye olay takibi yapabileceği bir kimlik vermiyordu. Login ve kayıt akışları brute-force veya otomasyon saldırılarına, AI analizi ise kontrolsüz maliyet tüketimine açıktı. Yönetim ve kimlik işlemleri için güvenlik olayı niteliğinde ortak bir audit izi de bulunmuyordu.

## Karar

1. Bütün API servisleri ve Gateway, RFC 7807 ProblemDetails biçimini kullanan ortak hata altyapısına bağlanacaktır.
2. Beklenmeyen exception ayrıntıları istemciye açılmayacak; istemciye destek ve log korelasyonu için `traceId` verilecektir.
3. Login, register ve external-login sınırları dış giriş noktası olan Gateway'de istemci IP'sine göre uygulanacaktır. Böylece iç ağdaki Auth servisi bütün kullanıcıları Gateway IP'si altında tek bir kota olarak görmez.
4. AI analizi için iki ayrı koruma uygulanacaktır: kısa süreli kullanıcı bazlı rate limit ve kullanıcı + UTC gün anahtarıyla SQLite'ta atomik UPSERT kullanılarak tutulan kalıcı günlük kota.
5. Günlük AI kotası, pahalı sağlayıcı çağrısından önce tüketilecektir. Sağlayıcı çağrısı başarısız olsa bile deneme kotaya dahildir; bu yaklaşım hata üzerinden maliyet saldırısını sınırlar.
6. Kimlik girişimleri, profil/yetki değişiklikleri, Maker ve ürün moderasyonu ile AI kota olayları ortak `IAuditLogger` üzerinden yapılandırılmış log olarak yazılacaktır.
7. Audit olaylarına token, parola, provider tokenı veya ham kişisel giriş verisi yazılmayacaktır.
8. Auth komutları FluentValidation ile endpoint sınırında doğrulanacak; AI analiz girdileri de sağlayıcıya gönderilmeden önce uzunluk ve zorunlu alan kontrollerinden geçecektir.

## Sonuçlar

### Olumlu

- Frontend bütün servislerde aynı hata sözleşmesini işleyebilir.
- `traceId` uygulama logu ile istemci hatasını ilişkilendirmeyi kolaylaştırır.
- Brute-force yüzeyi ve kontrolsüz AI maliyeti sınırlanır.
- AI günlük kullanım sayacı servis yeniden başladığında kaybolmaz.
- Kritik yönetim işlemleri sorgulanabilir, yapılandırılmış güvenlik olayları üretir.

### Maliyet ve kısıtlar

- Gateway'deki rate limit bellektedir; birden fazla Gateway replikasında sayaçlar paylaşılmaz.
- SQLite kota tablosu mevcut tek AI servisi dağıtımı için uygundur. Çoklu replica ve yüksek trafik aşamasında atomik dağıtık sayaç gerekir.
- Audit temeli uygulama loguna yazar; değiştirilemez ve uzun süre saklanan ayrı bir audit deposu henüz yoktur.
- Kota UTC gün sınırında yenilenir; kullanıcı saat dilimine göre günlük dönem uygulanmaz.

## Takip işleri

- Redis tabanlı dağıtık rate limiter ve atomik AI kota sayacı
- Audit olaylarını ayrı append-only depoya veya SIEM sistemine aktarma
- Rate limit, kota reddi ve authentication failure metrikleri
- ProblemDetails hata kodları için contract testleri
- API entegrasyon testlerinde 400, 401, 403, 404 ve 429 sözleşmelerini doğrulama
