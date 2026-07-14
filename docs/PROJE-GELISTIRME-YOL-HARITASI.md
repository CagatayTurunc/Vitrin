# Vitrin — Teknik Denetim ve Geliştirme Yol Haritası

> Denetim tarihi: 14 Temmuz 2026
> Durum: Mevcut çalışma ağacı üzerinden hazırlanmıştır.
> Amaç: Projeyi güvenli, test edilebilir, gözlemlenebilir, deploy edilebilir ve CV'de savunulabilir bir ürün hâline getirmek.

> Uygulama durumu: Aşama 2 tamamlandı. Event kararları [ADR-0004](adr/0004-event-teslimati-outbox-inbox.md), migration kararı [ADR-0005](adr/0005-migration-deployment-job.md), sorgu/indeks sözleşmesi [veri erişimi rehberinde](data-access-performance.md) kayıtlıdır.

## 1. Genel değerlendirme

Vitrin sıradan bir CRUD projesinden daha güçlü bir temele sahiptir. Projede Next.js frontend, .NET 8 servisleri, YARP API Gateway, PostgreSQL/SQLite, Kafka, Redis, EF Core migrations, Docker Compose, DDD ve CQRS yaklaşımı bulunmaktadır.

Mevcut hâliyle proje geniş kapsamlı ve iddialı bir MVP/öğrenme projesidir. Ancak “production-ready mikroservis mimarisi” olarak sunulmadan önce güvenlik, event akışı, veri sahipliği, test zinciri, deployment ve observability alanlarındaki temel borçlar kapatılmalıdır.

Yaklaşık olgunluk değerlendirmesi:

| Alan | Mevcut durum |
|---|---:|
| Ürün kapsamı | 6/10 |
| Frontend ve UI | 5/10 |
| Backend doğruluğu | 4/10 |
| Güvenlik | 2/10 |
| Veritabanı ve sorgular | 4/10 |
| Event-driven mimari | 2/10 |
| Test | 3/10 |
| Deployment ve observability | 3/10 |
| Dokümantasyon ve CV sunumu | 2/10 |

## 2. Projenin güçlü tarafları

- Servislerde Domain, Application, Infrastructure ve API katmanları ayrılmıştır.
- Auth, Product, Voting, Comment, Notification, Analytics ve AI alanları düşünülmüştür.
- YARP Gateway ve servis bazlı route yapısı bulunmaktadır.
- EF Core migrations kullanılmaktadır.
- Dockerfile'lar multi-stage build kullanmaktadır.
- Container'lar non-root kullanıcıyla çalışacak şekilde hazırlanmıştır.
- Health check ve Docker Compose orkestrasyonu bulunmaktadır.
- Kafka tabanlı asenkron iletişim hedeflenmiştir.
- Domain invariant'ları ve Result modeli için temel bulunmaktadır.
- Auth, Product ve Voting için anlamlı unit testler yazılmıştır.
- Frontend yalnızca tek sayfalık bir demo değildir; profil, admin, ürün gönderme, arama, koleksiyon, yorum, takip ve leaderboard gibi çok sayıda akış düşünülmüştür.

Bu temel doğru şekilde sağlamlaştırılırsa proje güçlü bir portföy çalışmasına dönüşebilir.

## 3. Kritik güvenlik ve doğruluk sorunları

### 3.1 OAuth hesap ele geçirme riski

`external-login` endpoint'i Google veya GitHub token'ını backend tarafında doğrulamamaktadır. İstemciden gelen `email`, `providerId` ve `provider` alanlarına güvenmektedir. Gönderilen e-posta mevcut bir kullanıcıya aitse bu kullanıcı için JWT üretilmektedir.

Bu endpoint Gateway üzerinden doğrudan çağrılabildiği için NextAuth tarafında OAuth yapılması tek başına koruma sağlamaz.

Yapılması gerekenler:

- Google ID token ve GitHub access token backend tarafında doğrulanmalıdır.
- İmza, issuer, audience, expiry ve verified-email kontrol edilmelidir.
- OAuth account-linking ayrı ve yeniden kimlik doğrulama gerektiren bir akış olmalıdır.
- Provider tarafından doğrulanmayan e-posta için JWT üretilmemelidir.
- Bu akış için güvenlik ve integration testleri yazılmalıdır.

### 3.2 Authentication var, tutarlı authorization yok

Gateway JWT authentication middleware'i kullanmaktadır; ancak route'larda `RequireAuthorization` veya YARP `AuthorizationPolicy` bulunmamaktadır. Authentication middleware'in varlığı bir endpoint'i otomatik olarak korumaz.

Ayrıca bazı endpoint'lerde:

- Ürün oluştururken `MakerId` body'den alınmaktadır.
- Yorum eklerken `UserId` ve `UserName` body'den alınmaktadır.
- Maker başvurusunda `UserId` body'den alınmaktadır.
- Koleksiyon oluşturma ve koleksiyona ürün ekleme/silme akışlarında sahiplik doğrulaması yoktur.
- Voting servisi kullanıcı kimliğini komuttan kabul etmektedir.
- Bildirimler URL'deki herhangi bir `userId` ile okunabilmektedir.
- Analytics ve AI endpoint'lerinin önemli bölümü herkese açıktır.
- Bazı endpoint'lerde JWT doğrulanmak yerine yalnızca `ReadJwtToken()` ile decode edilmektedir.
- Gamification endpoint'i gerçek bir oy işlemi oluştuğunu doğrulamamaktadır.

Hedef güvenlik modeli:

- Kullanıcı kimliği yalnızca doğrulanmış `ClaimsPrincipal` üzerinden alınmalıdır.
- `UserId`, `MakerId` ve `Role` gibi güvenlik alanları request body'den kabul edilmemelidir.
- Policy-based authorization kullanılmalıdır: `AdminOnly`, `MakerOrAdmin`, `ResourceOwner`.
- Gateway tek güvenlik sınırı sayılmamalıdır; servisler de token veya internal-service kimliğini doğrulamalıdır.
- Collection, notification, comment ve profile endpoint'leri için IDOR testleri yazılmalıdır.

### 3.3 Hardcoded secret ve kimlik doğrulama eksikleri

- JWT için kaynak kodda varsayılan secret bulunmaktadır.
- PostgreSQL için `123456` varsayılan şifresi bulunmaktadır.
- NextAuth için varsayılan secret bulunmaktadır.
- Login/register rate limiting yoktur.
- Account lockout yoktur.
- Email verification ve password reset yoktur.
- Refresh token rotation, token revocation ve session yönetimi yoktur.
- Güvenli secret manager entegrasyonu yoktur.

## 4. Kafka ve event-driven mimari sorunları

### 4.1 Topic mapping hatası

Bütün event tipleri `Vitrin.Shared.Contracts.Events` namespace'i altındadır. `KafkaProducer`, topic adını namespace'in ikinci parçasından üretmektedir. Bu nedenle event'ler `shared-events` topic'ine gitmektedir.

Consumer'lar ise şu topic'leri dinlemektedir:

- `voting-events`
- `notification-events`
- `analytics-events`

Bu nedenle event publish logları başarılı görünse bile consumer'ların mesajları almaması beklenir.

Yapılması gerekenler:

- Event → topic mapping açık bir sözlük, attribute veya publisher parametresiyle yapılmalıdır.
- Topic mapping contract test ile doğrulanmalıdır.
- Event isimleri ve topic'ler için bir event catalog hazırlanmalıdır.

### 4.2 Outbox/Inbox eksikliği

Database transaction tamamlandıktan sonra Kafka publish işlemi başarısız olabilir. Bazı publisher'lar hatayı loglayıp yutmaktadır. Böyle bir durumda veri DB'ye yazılır fakat event kalıcı olarak kaybolur.

Gerekli yapı:

- Transactional Outbox
- Inbox/ProcessedMessages tablosu
- Event ID bazlı idempotency
- Exponential retry ve backoff
- Poison-message politikası
- Dead Letter Queue
- Schema version
- Backward compatibility politikası
- Correlation ve causation ID

### 4.3 Voting için iki veri otoritesi

Oylama şu anda iki ayrı yerde tutulmaktadır:

- Product DB: `ProductUpvotes`
- Voting DB: `Votes`

Frontend Product servisindeki vote endpoint'ini kullanmaktadır. Buna rağmen ayrı Voting servisi ve Product tarafında Voting event consumer'ı bulunmaktadır.

Tek authoritative source seçilmelidir:

1. Voting/Engagement servisi oylamanın sahibi olur, Product yalnızca read-model/counter tutar; veya
2. Voting servisi kaldırılır ve oylama Product/Engagement modülünde tutulur.

İki servis aynı kavram üzerinde write yapmamalıdır.

## 5. Frontend eksikleri

### 5.1 API erişimi dağınık

- Bazı çağrılar hardcoded `http://localhost:5000` kullanmaktadır.
- Bazı çağrılar `NEXT_PUBLIC_API_URL` kullanmaktadır.
- Notification store fallback olarak `http://localhost:5177` kullanmaktadır; bu Product servisi portudur.
- Server Component olan Settings sayfası public/browser URL'sini kullanmaktadır. Docker içinde `localhost`, Gateway yerine frontend container'ını ifade eder.
- Axios, native fetch ve Zustand içinde farklı error-handling biçimleri bulunmaktadır.

Hedef:

- Tek bir typed API client oluşturulmalıdır.
- Browser ve server-side base URL'leri ayrılmalıdır.
- OpenAPI'den TypeScript DTO/client üretimi yapılmalıdır.
- Authentication header, correlation ID, timeout ve hata dönüşümü merkezi olmalıdır.

### 5.2 Type safety ve kalite kapısı

- Çok fazla `any` kullanılmaktadır.
- TypeScript build hataları `ignoreBuildErrors: true` ile gizlenmektedir.
- ESLint bağımlılığı bulunmadığı için `npm run lint` çalışmamaktadır.
- Frontend unit/component testi yoktur.
- E2E testi yoktur.

Yapılması gerekenler:

- `ignoreBuildErrors` kaldırılmalıdır.
- ESLint ve Next.js kuralları kurulmalıdır.
- Strict TypeScript ve API DTO'ları kullanılmalıdır.
- Vitest + React Testing Library kurulmalıdır.
- Playwright E2E testleri eklenmelidir.
- axe accessibility kontrolleri CI'a bağlanmalıdır.

### 5.3 Rendering, SEO ve performans

- Product detail büyük ölçüde client-rendered'dır.
- Dinamik metadata ve Open Graph bilgileri eksiktir.
- `loading.tsx`, `error.tsx` ve `not-found.tsx` yapısı eksiktir.
- Image optimization kapatılmıştır.
- Bazı optimistic update işlemlerinde rollback yoktur.
- Notification sistemi 15 saniyelik polling kullanmaktadır.

Hedef:

- Ürün detay ve public profil sayfaları server-rendered olmalıdır.
- Dynamic metadata, JSON-LD, sitemap ve canonical URL eklenmelidir.
- SignalR veya SSE ile gerçek zamanlı bildirim eklenmelidir.
- Web Vitals ölçülmeli ve performans bütçesi tanımlanmalıdır.

### 5.4 Eksik/placeholder UI özellikleri

- Takip bırakma UI'ı doğru DELETE isteğini göndermemektedir.
- “Geliştirici Sitesi” butonu aktif bir link değildir.
- Product submit formundaki website alanı backend modeline tam taşınmamaktadır.
- Paylaş ve şikayet et butonları işlevsel değildir.
- Şifre değiştirme placeholder'dır.
- Bildirim tercihleri placeholder'dır.
- Hesap silme placeholder'dır.
- Bazı blog/events/careers/discussions içerikleri ürün özelliği olmaktan çok statik sayfa seviyesindedir.
- Cloudinary yüklemelerinde backend doğrulaması, dosya boyutu ve kota kontrolü yoktur.

## 6. Backend ve API eksikleri

- `Program.cs` dosyaları endpoint, EF sorgusu ve iş kurallarını aynı yerde toplamaktadır.
- Request validation katmanı yoktur.
- Merkezi exception handling yoktur.
- RFC 7807 `ProblemDetails` standardı yoktur.
- HTTP status code kullanımı tutarlı değildir.
- Pagination/filter/sorting standardı yoktur.
- Cancellation token'lar tutarlı şekilde taşınmamaktadır.
- Idempotency key desteği yoktur.
- API versioning yoktur.
- OpenAPI contract'ı release sürecinin parçası değildir.
- Audit log yoktur.
- Feature flag altyapısı yoktur.

Önerilen yapı:

- Endpoint group veya controller/vertical-slice dosyaları
- FluentValidation veya endpoint filter
- Global exception handler
- Ortak `ProblemDetails` hata kodları
- Cursor pagination
- API versioning
- Idempotency middleware
- OpenAPI breaking-change kontrolü
- Audit trail

## 7. Veritabanı ve sorgu eksikleri

### 7.1 Gerekli indeksler

- `ProductUpvotes(ProductItemId, UserId)` unique index
- `Products(Status, PublishedAt)`
- `Comments(ProductId, CreatedAt)`
- `Comments(ParentCommentId)`
- `Notifications(UserId, IsRead, CreatedAt)`
- `AnalyticsEvents(EventType, ProductId, CreatedAt)`
- `UserFollows(FollowingId)`
- AI analizinde ihtiyaca göre unique `ProductId`

### 7.2 Query ve ölçeklenebilirlik sorunları

- Products, comments, users, collections ve notifications sayfalanmadan `ToListAsync` yapmaktadır.
- Search `ToLower().Contains()` kullanmaktadır; relevance ve indeks kullanımı zayıftır.
- Analytics top-search bütün JSON event verisini belleğe alıp gruplamaktadır.
- Platform analytics birden çok ardışık count sorgusu çalıştırmaktadır.
- Admin maker başvuruları bütün kullanıcıları çekip bellekte eşleştirmektedir.
- Read sorgularında `AsNoTracking` kullanılmamaktadır.
- Slug üretimindeki `while AnyAsync` yarış koşuluna açıktır.
- Soft-delete yorum içeriği API cevabında dönmeye devam etmektedir.
- AI analizinde aynı ürün için çok sayıda satır üretilebilir.
- Batch product endpoint'inde maksimum ID sayısı yoktur.

Hedef:

- Cursor pagination
- Projection DTO
- `AsNoTracking`
- PostgreSQL full-text search veya `pg_trgm`
- Query plan ve `EXPLAIN ANALYZE` dokümantasyonu
- Composite/partial indexler
- Optimistic concurrency token
- Transaction/retry politikası
- Analytics için aggregate/read-model tabloları

## 8. Test durumu

Denetim sırasında alınan sonuçlar:

- Auth: 26 test geçti.
- Product: 24 test geçti.
- Voting: 7 test geçti.
- Toplam 57 test geçti.
- Comment test projesi eski constructor imzası nedeniyle derlenemedi.
- Bu nedenle `dotnet test Vitrin.sln` genel sonucu başarısızdır.
- Frontend `npm run lint`, ESLint bulunmadığı için başarısızdır.
- Frontend test altyapısı bulunmamaktadır.

Hedef test piramidi:

### Unit test

- Domain invariant'ları
- Command/query handler'ları
- Authorization kararları
- Ranking algoritmaları
- Event-topic mapping

### Integration test

- Testcontainers PostgreSQL
- Testcontainers Kafka
- Testcontainers Redis
- EF migrations
- Repository ve index davranışları
- Outbox dispatcher ve Inbox idempotency

### API test

- `WebApplicationFactory`
- Authenticated/unauthenticated endpoint senaryoları
- IDOR testleri
- ProblemDetails contract'ı
- Rate-limit ve validation testleri

### Contract test

- OpenAPI compatibility
- Kafka event schema compatibility
- Producer/consumer topic eşleşmesi

### E2E test

- Register/login
- Maker başvurusu
- Admin onayı
- Ürün gönderme
- Ürün onayı
- Vote/unvote
- Comment/reply/edit/delete
- Collection oluşturma
- Notification görme

### Non-functional test

- k6 load test
- OWASP ZAP baseline
- Accessibility/axe
- Lighthouse/Web Vitals
- Backup/restore testi

## 9. DevOps ve deployment eksikleri

Olumlu taraflar:

- Multi-stage Docker build
- Non-root container
- Health check
- Docker Compose
- `docker compose config` doğrulaması başarılı

Eksikler:

- CI/CD workflow yoktur.
- Staging ve production ayrımı yoktur.
- Production ingress/TLS/domain yapısı yoktur.
- Secret manager yoktur.
- Infrastructure as Code yoktur.
- Backup/restore otomasyonu ve testi yoktur.
- Rollback stratejisi yoktur.
- Container vulnerability scan ve SBOM yoktur.
- Dependency/security scan yoktur.
- CPU/memory limitleri yoktur.
- Startup migration birden fazla instance'ta yarışabilir.
- Smoke test Docker Compose portlarıyla uyuşmamaktadır.
- SQLite bind mount'larının non-root kullanıcı tarafından yazılabilirliği garanti değildir.

Hedef pipeline:

1. Restore/install
2. Format/lint/typecheck
3. Unit test
4. Integration test
5. Frontend/backend build
6. OpenAPI ve event contract kontrolü
7. Dependency ve secret scan
8. Container build
9. Image vulnerability scan
10. SBOM üretimi
11. Staging deploy
12. Smoke/E2E test
13. Manuel veya korumalı production promotion
14. Migration job
15. Rollback doğrulaması

Kubernetes yalnızca gerçekten öğrenme/deployment hedefi varsa eklenmelidir. Tek bir iyi yönetilmiş cloud deployment + Terraform, yarım bir Kubernetes kurulumundan daha değerlidir.

## 10. Observability eksikleri

Şu anda temel console log ve health endpoint'leri bulunmaktadır. Hedef:

- OpenTelemetry trace, metric ve log
- Gateway'den bütün servislere correlation ID
- Causation ID ve event ID
- Prometheus/Grafana
- Tempo/Jaeger
- Loki/Seq
- Liveness/readiness ayrımı
- Kafka consumer lag metriği
- DB connection pool metriği
- HTTP p50/p95/p99 latency
- Error rate ve saturation
- AI request latency/token/cost metriği
- SLO/SLI ve alert tanımları

Örnek SLO'lar:

- Public read API availability: %99.9
- Product list p95: ölçümle belirlenecek hedef
- Vote command success rate: %99.9
- Notification event processing delay: ölçümle belirlenecek hedef
- Kafka DLQ büyümesi: kritik alarm

Bu değerler ölçülmeden CV'de sayı olarak kullanılmamalıdır.

## 11. Kabul edilen hedef mimari

Projenin mikroservis mimarisini öğrenme ve CV üzerinde gösterme hedefi nedeniyle mevcut servis sayısının korunmasına karar verilmiştir. Aşama 0 kapsamında hiçbir servis birleştirilmeyecek veya kaldırılmayacaktır. Kararın bağlamı ve sonuçları [ADR-0001](adr/0001-mikroservis-mimarisini-koruma.md) belgesinde kayıtlıdır.

Servis sayısını korumak, her klasörü otomatik olarak gerçek bir mikroservis yapmaz. Her servis için şu şartlar sağlanmalıdır:

- Tek ve açık veri sahipliği
- Ayrı migration yönetimi
- OpenAPI contract
- Event catalog
- Outbox/Inbox
- Contract test
- Trace/metric/log
- Bağımsız build ve deploy
- Runbook ve SLO

Voting servisi oyların tek yazma otoritesi olacaktır. Product servisi oy verisini event-driven read model olarak tüketebilir; iki servis aynı oyu bağımsız olarak yazmayacaktır.

En önemli CV çıktısı kullanılan teknoloji sayısı değil; servis sınırlarını neden seçtiğini ve dağıtık sistem maliyetlerini nasıl yönettiğini açıklayan ADR, test ve ölçüm kayıtlarıdır.

## 12. Eklenebilecek güçlü ürün özellikleri

### Product lifecycle

- Draft
- UnderReview
- Rejected + rejection reason
- Scheduled
- Published
- Archived
- Revision/history
- Admin review notes
- Product ownership/claim
- Maker takım üyeleri
- Product changelog

### Discovery ve search

- Günlük/haftalık launch sayfaları
- Time-decay trending algoritması
- Full-text search
- Typo tolerance
- Semantic search
- Gelişmiş filtreleme
- Cursor pagination
- Saved search
- Takip edilen topic'ler
- Product comparison
- Açıklanabilir recommendations

### Community

- Comment reactions
- Mentions
- Nested discussion iyileştirmeleri
- Activity feed
- Report/appeal
- Spam koruması
- Moderation queue
- Reputation sistemi
- Server-side gamification
- Badge rule engine
- Real-time notifications
- Email digest

### Collections

- Public/private/unlisted visibility
- Collaborative collection
- Collection followers
- Collection cover ve metadata
- Sıralama ve not ekleme
- Duplicate önleme

### Maker dashboard

- Product views
- Unique visitors
- Referrer/UTM
- Vote/comment conversion
- Follower growth
- Retention
- Export CSV
- Launch campaign analizi

### AI

- Async AI analysis job
- Usage quota ve maliyet takibi
- Otomatik topic/tag üretimi
- Spam ve zararlı içerik sınıflandırması
- Duplicate product detection
- Embedding + pgvector
- Semantic search
- Recommendation explanation
- Prompt/model/version/latency/token kayıtları
- AI sonucuna kullanıcı feedback'i

### Privacy ve compliance

- KVKK consent kaydı
- Veri export
- Hesap ve veri silme
- Retention politikası
- Audit log
- Cookie preference yönetimi
- PII redaction

## 13. CV ve portföy sunumu

Root README şu bölümleri içermelidir:

- Problem ve ürün vizyonu
- Canlı demo
- Ekran görüntüleri ve kısa demo videosu
- Özellik matrisi
- Teknoloji yığını ve nedenleri
- C4 Context/Container/Component diyagramları
- ERD
- Request sequence diagramları
- Event sequence diagramları
- Event catalog
- OpenAPI bağlantıları
- Security threat model
- Test piramidi ve gerçek coverage sonucu
- k6 benchmark sonuçları
- Deployment diyagramı
- ADR'ler ve trade-off'lar
- Local development rehberi
- Runbook
- Backup/restore prosedürü
- Incident senaryosu
- Known limitations

Ölçülebilir CV cümlesi için şablon:

> Next.js ve .NET 8 ile ürün keşif platformu tasarladım; policy-based authorization, PostgreSQL full-text search, transactional outbox/inbox, Kafka tabanlı asenkron işlemler ve OpenTelemetry gözlemlenebilirliği uyguladım. Testcontainers ve Playwright ile kritik akışları doğruladım; k6 ölçümlerinde p95 gecikmesini **ölçülen değer** altında tuttum.

Coverage, throughput ve latency değerleri ölçülmeden yazılmamalıdır.

## 14. Repo ve dokümantasyon temizliği

- Root README bulunmamaktadır.
- İki farklı frontend scaffold'ı bulunmaktadır; yalnızca `src/Web/Vitrin.Web.UI` aktiftir.
- Hem `package-lock.json` hem `pnpm-lock.yaml` commit edilmiştir.
- `tsconfig.tsbuildinfo` commit edilmiştir.
- SQLite WAL/SHM dosyaları commit edilmiştir.
- Frontend altında geçici `query.sql`, `query2.sql`, `query3.sql`, `query4.sql` dosyaları bulunmaktadır.
- `.gitignore` sonunda farklı encoding ile eklenmiş bozuk/NUL karakterli satırlar bulunmaktadır.
- Placeholder klasör ve README'ler gerçek uygulamanın konumunu tarif etmektedir.
- Dokümantasyon Gateway'in her istekte JWT ile güvenli erişim sağladığını ve frontend'in production-ready olduğunu iddia etmektedir; mevcut kod bunu tam olarak desteklememektedir.
- Smoke test dokümantasyonu ve Docker Compose portları uyuşmamaktadır.

## 15. Uygulama sırası

### Aşama 0 — Stabilizasyon ve repo hijyeni

- [x] Mevcut çalışma ağacını güvenli bir checkpoint commit ile kaydet
- [x] Boş ikinci frontend scaffold'ını kaldır
- [x] Tek package manager seç
- [x] Gereksiz lockfile'ı kaldır
- [x] `.gitignore` encoding/NUL sorununu düzelt
- [x] SQLite WAL/SHM dosyalarını repodan çıkar
- [x] `tsconfig.tsbuildinfo` dosyasını repodan çıkar
- [x] Geçici query dosyalarını temizle veya doğru yere taşı
- [x] Root README oluştur
- [x] Comment test projesini derlenebilir hâle getir
- [x] ESLint ve frontend typecheck'i çalışır hâle getir
- [x] Bütün test/build komutlarını tek script veya Make/Task dosyasında topla

### Aşama 1 — Güvenlik ve temel doğruluk

- [x] OAuth external-login açığını kapat
- [x] Merkezi JWT authentication yapılandırması oluştur
- [x] Policy-based authorization ekle
- [x] Request body'den alınan UserId/MakerId alanlarını kaldır
- [x] Collection sahiplik kontrollerini ekle
- [x] Notification IDOR açığını kapat
- [x] Comment create kimlik doğrulamasını düzelt
- [x] Voting endpoint'lerini doğrulanmış kullanıcıya bağla
- [x] AI endpoint'ine auth, quota ve rate limit ekle
- [x] Analytics admin endpoint'lerini koru
- [x] Login/register rate limiting ekle
- [x] Secret fallback'lerini kaldır
- [x] Validation ve ProblemDetails ekle
- [x] Audit log temelini oluştur

### Aşama 2 — Mimari tutarlılık ve veri katmanı

- [x] Voting için tek authoritative source seç: Voting servisi
- [x] Mikroservis mimarisini koruma kararını ADR ile kaydet
- [x] Kafka topic mapping'i düzelt
- [x] Transactional Outbox ekle
- [x] Inbox/idempotency ekle
- [x] Retry/backoff ve DLQ ekle
- [x] Event schema versioning ekle
- [x] Gerekli DB indekslerini ekle
- [x] Cursor pagination ekle
- [x] Read sorgularına projection ve `AsNoTracking` ekle
- [x] PostgreSQL full-text search veya `pg_trgm` ekle
- [x] Slug concurrency problemini düzelt
- [x] Migration'ları uygulama startup'ından deployment job'a taşı

### Aşama 3 — Test mimarisi

- [x] Bütün unit testleri yeşile getir
- [ ] Testcontainers PostgreSQL testleri ekle
- [ ] Testcontainers Kafka testleri ekle
- [ ] Testcontainers Redis testleri ekle
- [ ] WebApplicationFactory API testleri ekle
- [ ] Authorization ve IDOR testleri ekle
- [x] Event-topic contract testleri ekle
- [ ] OpenAPI compatibility kontrolü ekle
- [ ] Vitest ve React Testing Library ekle
- [ ] Playwright kritik akışlarını ekle
- [ ] axe accessibility testleri ekle
- [ ] k6 load test senaryoları ekle
- [ ] Coverage threshold belirle

### Aşama 4 — Observability ve production deployment

- [ ] OpenTelemetry ekle
- [x] Correlation/causation ID standardı oluştur
- [ ] Prometheus/Grafana kur
- [ ] Trace backend'i kur
- [ ] Merkezi log backend'i kur
- [ ] Liveness/readiness ayır
- [ ] Kafka lag ve DLQ metriklerini ekle
- [ ] GitHub Actions CI oluştur
- [ ] Staging ortamı oluştur
- [ ] Production ortamı oluştur
- [ ] TLS/domain/ingress yapılandır
- [ ] Secret manager kullan
- [ ] Terraform/IaC ekle
- [ ] Backup/restore otomasyonu ve testi ekle
- [ ] Container scan ve SBOM ekle
- [ ] Rollback stratejisini doğrula
- [ ] SLO ve alert tanımla

### Aşama 5 — Portföyü farklılaştıran özellikler

- [ ] Trending algoritması
- [ ] Gelişmiş full-text search
- [ ] Semantic search
- [ ] Açıklanabilir recommendation
- [ ] SignalR/SSE real-time notification
- [ ] Maker analytics dashboard
- [ ] Moderation/report/appeal sistemi
- [ ] Audit log görüntüleme
- [ ] Product lifecycle ve revision history
- [ ] KVKK export/delete akışı
- [ ] Ölçülmüş performans raporu
- [ ] C4/ERD/sequence/event dokümantasyonu
- [ ] Canlı demo ve kısa demo videosu

## 16. İlk başlanacak konu

İlk teknik çalışma **Aşama 0 — Stabilizasyon ve repo hijyeni** olmalıdır. Ancak güvenlik açısından ilk fonksiyonel düzeltme **OAuth external-login açığıdır**.

Önerilen ilk sıra:

1. Çalışma ağacını güvenceye al.
2. Test/lint/typecheck zincirini yeşile getir.
3. OAuth açığını kapat.
4. Merkezi authorization kur.
5. Voting veri sahipliğini kararlaştır.
6. Kafka topic mapping ve Outbox/Inbox yapısını kur.

Bu altı adım tamamlandığında proje yeni özellik eklemeye uygun, güvenilir bir mimari temele kavuşacaktır.
