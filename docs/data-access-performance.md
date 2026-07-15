# Veri erişimi ve sorgu performansı

Bu belge Aşama 2 veri katmanı kararlarının ölçülebilir sözleşmesidir. Süre değerleri donanım ve veri hacmine bağlı olduğundan burada uydurma benchmark sayıları kullanılmaz; staging verisiyle alınan planlar ayrıca arşivlenmelidir.

## Cursor pagination sözleşmesi

`GET /api/products?pageSize=20&cursor=...` yanıtı:

```json
{
  "items": [],
  "nextCursor": "opaque-value-or-null",
  "hasMore": false
}
```

Sıralama `(PublishedAt DESC, Id DESC)` şeklindedir. Cursor bu iki anahtarı sürümlü, URL-safe Base64 biçiminde taşır. Offset kullanılmadığı için önceki sayfalara yeni kayıt eklenmesi, sonraki sayfalarda kayıt atlama veya tekrarlama üretmez. `pageSize` 1-100 aralığındadır; cursor geçersizse `400 pagination.invalid_cursor` döner.

## Projection ve tracking

Public product, topic, collection, comment, notification, AI ve analytics read yolları `AsNoTracking` ile çalışır. Product sorguları entity graph'ını `Include` ile belleğe almak yerine API DTO'suna SQL projection yapar. Batch product isteği en fazla 100 ID, arama en fazla 50 sonuç, yorum thread'i 500 ve notification listesi 100 kayıtla sınırlıdır.

Soft-delete edilmiş yorum içeriği API'den geri sızdırılmaz; projection `Content` alanını `[deleted]` olarak maskeler.

## PostgreSQL arama

Product araması `ToLower().Contains()` yerine escape edilmiş `ILIKE` kullanır. `pg_trgm` GIN indeksleri `Products.Name`, `Tagline`, `Description` ve `Topics.Name` alanlarını destekler. Sonuçlar tam ad eşleşmesi, ad prefix eşleşmesi ve yayın tarihiyle sıralanır.

Migration kullanıcısının `CREATE EXTENSION pg_trgm` yetkisine sahip olması gerekir. Yetkinin güvenlik ekibi tarafından önceden verilmesi tercih edilir.

Auth servisinde email ve username sütunları PostgreSQL `citext` kullanır. Böylece case-insensitive eşitlik ve unique kontrolleri `lower(column)` fonksiyonu olmadan B-tree indekslerini kullanır. Migration, yalnızca harf büyüklüğüyle ayrılan mevcut çakışmaları veri silmeden tespit eder ve açık hata ile durur. Migration kullanıcısı için `citext` extension yetkisi de önceden sağlanmalıdır.

## İndeks-query eşleşmesi

| Sorgu | İndeks |
|---|---|
| Published product cursor | `IX_Products_Status_PublishedAt_Id` |
| Maker products | `IX_Products_MakerId` |
| Product vote read model | `UX_ProductUpvotes_ProductId_UserId` |
| User collections | `IX_Collections_UserId_CreatedAt` |
| Product comments | `IX_Comments_ProductId_CreatedAt_Id` |
| Comment replies | `IX_Comments_ParentCommentId` |
| User notifications/unread | `IX_Notifications_UserId_IsRead_CreatedAt_Id` |
| Product vote count | `IX_Votes_ProductId_CreatedAt` |
| Analytics counters | `IX_AnalyticsEvents_EventType_ProductId_CreatedAt` |
| Followers | `IX_UserFollows_FollowingId_CreatedAt` |
| Case-insensitive login/profile lookup | `UX_Users_Email`, `UX_Users_Username` (`citext`) |
| OAuth provider subject lookup | `UX_Users_GoogleId`, `UX_Users_GithubId` |
| Pending maker applications | `IX_MakerApplications_Status_CreatedAt` |
| AI product history | `IX_AiAnalysisResults_ProductId_AnalyzedAt` |

## EXPLAIN ANALYZE kontrolü

Staging'de production'a yakın veri hacmiyle aşağıdaki biçimde plan alınmalıdır:

```sql
EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT "Id", "Name", "PublishedAt"
FROM "Products"
WHERE "Status" = 2
  AND ("PublishedAt", "Id") < (TIMESTAMPTZ '2026-07-14T12:00:00Z', '00000000-0000-0000-0000-000000000000')
ORDER BY "PublishedAt" DESC, "Id" DESC
LIMIT 21;
```

```sql
EXPLAIN (ANALYZE, BUFFERS, VERBOSE)
SELECT "Id", "Name"
FROM "Products"
WHERE "Status" = 2 AND "Name" ILIKE '%ai%'
ORDER BY "PublishedAt" DESC
LIMIT 20;
```

Kontrol listesi: beklenen indeks adı, actual/estimated row farkı, shared/read hit oranı, sort spill, sequential scan gerekçesi ve p95 API süresi. Küçük tablolarda planner'ın sequential scan seçmesi tek başına hata değildir.
