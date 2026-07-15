# ADR-0004: Güvenilir event teslimatı, Outbox ve Inbox

- Durum: Kabul edildi
- Tarih: 2026-07-14
- Karar sahipleri: Vitrin geliştirme ekibi

## Bağlam

Event topic'i namespace'ten türetildiği için bütün mesajlar fiilen `shared-events` topic'ine yönleniyor, consumer'lar ise semantik topic'leri dinliyordu. Ayrıca servis verisi kaydedildikten sonra Kafka publish işlemi başarısız olduğunda event kaybolabiliyor; tekrar teslim edilen mesajlar da aynı iş sonucunu birden fazla kez üretebiliyordu. Product ve Voting'in aynı oy için ayrı yazma yollarına sahip olması veri sahipliği sınırını belirsizleştiriyordu.

## Karar

1. Voting, oyların tek yazma otoritesidir. İstemci oy ekleme/silme komutlarını Voting API'ye gönderir. Product yalnızca `voting-events` üzerinden oluşturduğu oy read modelini sorgular.
2. Event-topic ilişkisi namespace veya sınıf adı tahmininden üretilmez. Bütün event türleri `EventCatalog` içinde açıkça kayıtlıdır ve kayıt tamlığı contract testiyle doğrulanır.
3. Domain değişikliği ile event üretiminin birlikte gerçekleştiği Auth, Product, Voting ve Comment servisleri ortak Transactional Outbox modelini kullanır. İş verisi ve Outbox satırı aynı `DbContext.SaveChanges` transaction'ında kalıcılaşır.
4. Product, Analytics ve Notification consumer'ları iş sonucu ile Inbox satırını aynı yerel veritabanı transaction'ında kaydeder. Inbox primary key'i integration event ID'dir; tekrar teslimat iş kuralını yeniden çalıştırmaz.
5. Teslimat garantisi **at-least-once**'tır. Producer idempotence açıktır; consumer offset'i yalnızca işleme veya DLQ'ya taşıma başarıyla bittikten sonra commit edilir.
6. Consumer işlemesi sınırlı sayıda exponential backoff ile tekrar denenir. Son deneme de başarısızsa orijinal mesaj ve hata metadatası `<topic>.dlq` topic'ine yazılır; DLQ publish başarısızsa aynı offset yeniden denenir.
7. Event envelope alanları `EventId`, `EventType`, `Timestamp`, `Version`, `CorrelationId` ve opsiyonel `CausationId` olarak standartlaştırılır. Aynı metadata Kafka header'larında da taşınır.
8. Event sürümü `major.minor` biçimindedir. Aynı major sürümde yalnızca geriye uyumlu, opsiyonel alan eklenebilir. Alan silme, anlam değiştirme veya tip değiştirme breaking change sayılır; yeni major event sözleşmesi paralel consumer geçişiyle yayınlanır. Event type adı yayınlandıktan sonra yeniden kullanılmaz.

## Topic sınırları

| Topic | İçerik | Temel consumer |
|---|---|---|
| `voting-events` | Oy ekleme ve geri alma | Product, Analytics |
| `notification-events` | Bildirim gönderme isteği | Notification |
| `analytics-events` | Görüntüleme, arama ve analitik sinyaller | Analytics |
| `social-events` | Ürün yayınlama ve yorum sosyal olayları | Analytics ve gelecek sosyal read modeller |
| `user-events` | Kullanıcı yaşam döngüsü ve rol değişikliği | Analytics ve gelecek kullanıcı read modeller |

Ayrıntılı producer/consumer matrisi [Event Catalog](../event-catalog.md) belgesindedir.

## Sonuçlar

### Olumlu

- DB commit başarılı, Kafka publish başarısız senaryosunda event kaybolmaz.
- Kafka'nın tekrar teslimi oy sayısı, analitik kayıt veya bildirimi çoğaltmaz.
- Voting ve Product arasında açık CQRS/veri sahipliği sınırı oluşur.
- Poison message tek partition'ı sonsuza kadar bloke etmez; DLQ üzerinden incelenebilir.
- Event routing ve envelope davranışı testle korunur.

### Maliyet ve kısıtlar

- Outbox dispatcher şu anda satır kiralama/claim mekanizması kullanmaz. Aynı servisin birden fazla replikası aynı event'i publish edebilir; at-least-once tasarımı ve consumer Inbox'ları bu kopyaları güvenli kılar. Yüksek ölçek aşamasında PostgreSQL için `FOR UPDATE SKIP LOCKED` veya lease alanları eklenecektir.
- DLQ mesajları otomatik replay edilmez. Replay öncesi kök neden giderilmeli ve event ID korunmalıdır.
- Inbox tablosu için saklama/temizleme politikası henüz uygulanmamıştır. Retention süresi, Kafka retention ve replay penceresinden uzun seçilmelidir.
- Event şemaları JSON'dur; registry tabanlı uyumluluk kapısı henüz yoktur. CI aşamasında schema compatibility kontrolü eklenecektir.
- Product oy read modeli eventual consistent'tır; komut başarı yanıtından hemen sonra kısa süre eski değer görülebilir.

## Operasyon kuralları

- Outbox'ta `ProcessedAtUtc` boş kalan satırlar ve `DeadLetteredAtUtc` dolan satırlar alarm üretmelidir.
- Kafka DLQ büyümesi ve consumer lag izlenmelidir.
- DLQ replay yeni bir event ID üretmemeli; Inbox idempotency anahtarı korunmalıdır.
- Event payload'ına token, parola veya gereksiz kişisel veri yazılmamalıdır.

## Takip işleri

- Çoklu replica için Outbox claim/lease mekanizması
- Inbox ve işlenmiş Outbox satırları için retention job'ı
- DLQ görüntüleme ve kontrollü replay aracı
- Kafka/Testcontainers entegrasyon testleri
- CI'da JSON schema compatibility kontrolü
- Consumer lag, retry, Outbox yaşı ve DLQ metrikleri
