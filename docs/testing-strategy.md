# Vitrin test stratejisi

Bu belge, testlerin hangi riski kapattığını, nerede çalıştığını ve bir değişikliğin hangi kalite kapılarından geçmesi gerektiğini tanımlar. Hedef yalnızca test sayısını büyütmek değil; kimlik sınırı, veri bütünlüğü, event teslimatı ve kullanıcı akışlarındaki regresyonları doğru katmanda yakalamaktır.

## Test piramidi

| Katman | Araç | Kapsanan risk | Çalıştırma |
|---|---|---|---|
| Unit | xUnit, Vitest, React Testing Library | Domain kuralları, handler/store ve component davranışı | Her değişiklikte |
| Integration | Testcontainers PostgreSQL/Kafka/Redis | Migration, constraint, index, cache TTL/invalidation, retry ve DLQ | PR kalite kapısında |
| API/security | WebApplicationFactory | JWT, policy, caller identity, ownership/IDOR, ProblemDetails | PR kalite kapısında |
| Contract | OpenAPI baseline ve event-topic testleri | Geriye uyumsuz API/event değişiklikleri | PR kalite kapısında |
| E2E/accessibility | Playwright ve axe-core | Gerçek tarayıcı smoke akışı ve WCAG A/AA ihlalleri | Compose/staging üzerinde |
| Load | k6 | Public read endpoint hata oranı ve p95 gecikmesi | Kontrollü yerel/staging koşusunda |

## Backend testleri

Unit testleri `Category!=Integration&Category!=Contract` filtresiyle dış bağımlılık olmadan çalışır. Integration testleri Docker üzerinde geçici bağımlılıklar oluşturur ve test sonunda temizler:

- PostgreSQL 16: migration uygulanabilirliği, `citext`, `pg_trgm`, bileşik/GIN indeksler ve unique constraint'ler.
- Kafka 7.6: topic/header/payload teslimatı, bounded retry ve poison message DLQ davranışı.
- Redis 7: serialization round-trip, TTL, exact key ve pattern invalidation.
- WebApplicationFactory: gerçek ASP.NET Core pipeline, JWT üretimi, 401/403 ayrımı, sahiplik/IDOR ve RFC 7807 `traceId` sözleşmesi.

OpenAPI testleri `tests/Vitrin.IntegrationTests/Contracts` altındaki baseline dosyalarını karşılaştırır. Baseline yalnızca değişiklik bilinçli biçimde incelendikten ve istemci etkisi değerlendirildikten sonra güncellenmelidir; testi geçirmek için otomatik üzerine yazılmaz.

```powershell
dotnet test Vitrin.sln --filter "Category!=Integration&Category!=Contract"
dotnet test tests\Vitrin.IntegrationTests\Vitrin.IntegrationTests.csproj --filter "Category=Integration|Category=Contract"
pwsh scripts\check-coverage.ps1
```

Backend coverage kapısı satır bazında en az `%35`'tir. Raporlar birden fazla test assembly'sindeki aynı kaynak satırlarını tekilleştirir ve `artifacts/backend-coverage` altında Cobertura çıktısı üretir. Eşik, yeni anlamlı testler geldikçe düşürülmeden kademeli artırılır.

## Frontend testleri

Vitest ve React Testing Library testleri kullanıcı davranışını esas alır. Mevcut kritik kapsam; credential/OAuth girişi, güvenli callback, parola görünürlüğü erişilebilirliği, kategori filtreleme, authenticated/unauthenticated oy davranışı ve ProblemDetails mesaj dönüşümüdür.

```powershell
cd src\Web\Vitrin.Web.UI
corepack pnpm test
corepack pnpm test:coverage
corepack pnpm test:e2e
```

Frontend coverage kapısı seçilmiş kritik modüllerde en az `%70` satır/function/statement ve `%60` branch'tir. Playwright varsayılan olarak `http://127.0.0.1:3000` adresini kullanır; başka ortam için `PLAYWRIGHT_BASE_URL` verilir. Başarısız E2E testlerinin screenshot, video ve trace dosyaları `artifacts/playwright-*` altında tutulur.

axe testleri `/` ve `/login` sayfalarında WCAG 2 A/AA ve 2.1 A/AA etiketlerini tarar; `critical` veya `serious` ihlal kalite kapısını düşürür. İhlaller testten hariç bırakılmak yerine kaynak kodda düzeltilir veya istisna gerekçesi belgelenir.

## Load testi

`tests/load/products-smoke.js`, Gateway üzerinden `GET /api/products` public read yolunu ölçer. Varsayılan güvenli smoke profili 5 sanal kullanıcı ve 15 saniyedir:

- HTTP hata oranı `< %1`
- check başarı oranı `> %99`
- p95 yanıt süresi `< 750 ms`

```powershell
pwsh scripts\run-load-test.ps1
pwsh scripts\run-load-test.ps1 -VirtualUsers 10 -Duration 30s
```

Load testi geliştirici makinesindeki kapasite yarışını ölçmemesi için büyük production build veya integration suite ile aynı anda çalıştırılmaz. CV/README'de gecikme sonucu yazılacaksa ortam, tarih, VU, süre ve ölçülen p95 değeriyle birlikte raporlanır.

15 Temmuz 2026 yerel Compose ölçümünde 5 VU/10 saniyelik sıcak koşu 129 istek, `%0` hata, `%100` check başarısı ve `371,92 ms` p95 üretti. Aynı gün build/ağ baskısı altındaki ilk soğuk koşu `5,22 sn` p95 ile eşiği geçemedi; bu sonuç cold-start ve kaynak izolasyonu çalışması için saklanmalıdır.

## Tek komut kalite kapısı

```powershell
pwsh scripts\verify.ps1 -Coverage -E2E -CheckCompose
```

Hızlı yerel döngüde `-SkipIntegration`, `-SkipBuild` veya `-SkipInstall` kullanılabilir. Merge öncesi tam kapıda integration testleri atlanmaz. Aşama 4 CI çalışmasında bu komut ayrı unit, integration, E2E ve load job'larına bölünecek; raporlar pipeline artifact'i olarak saklanacaktır.
