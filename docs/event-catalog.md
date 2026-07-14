# Vitrin Event Catalog

Bu belge servisler arası integration event sözleşmelerinin tek bakışta görülen envanteridir. Çalışan kod için authoritative mapping `Vitrin.Shared.Contracts.Events.EventCatalog` sınıfıdır; bu tablo mimari ve operasyonel açıklamayı taşır.

| Event type | CLR contract | Sürüm | Topic | Producer | Consumer / sonuç |
|---|---|---:|---|---|---|
| `voting.vote_added` | `VoteAddedEvent` | 1.0 | `voting-events` | Voting Outbox | Product oy read modeli, Analytics upvote metriği |
| `voting.vote_removed` | `VoteRemovedEvent` | 1.0 | `voting-events` | Voting Outbox | Product oy read modeli, Analytics downvote metriği |
| `notification.send` | `SendNotificationEvent` | 1.0 | `notification-events` | Auth/Comment Outbox | Notification kaydı |
| `analytics.product_viewed` | `ProductViewedEvent` | 1.0 | `analytics-events` | Web/API analitik producer | Analytics görüntüleme kaydı |
| `analytics.product_upvoted` | `ProductUpvotedEvent` | 1.0 | `analytics-events` | Geriye uyumluluk sözleşmesi; yeni oy akışı Voting event'ini kullanır | Analytics oy metriği |
| `analytics.search_performed` | `SearchPerformedEvent` | 1.0 | `analytics-events` | Arama akışı | Analytics arama kaydı |
| `analytics.comment_created` | `CommentCreatedAnalyticsEvent` | 1.0 | `analytics-events` | Yorum akışı | Analytics yorum kaydı |
| `product.published` | `ProductPublishedEvent` | 1.0 | `social-events` | Product Outbox | Analytics ürün yayın kaydı, gelecek sosyal read modeller |
| `comment.added` | `CommentAddedEvent` | 1.0 | `social-events` | Yorum akışı için sözleşme | Gelecek sosyal read modeller |
| `comment.replied` | `CommentRepliedEvent` | 1.0 | `social-events` | Yorum akışı için sözleşme | Gelecek sosyal read modeller |
| `user.registered` | `UserRegisteredEvent` | 1.0 | `user-events` | Auth yaşam döngüsü için sözleşme | Analytics kullanıcı kaydı |
| `user.role_changed` | `UserRoleChangedEvent` | 1.0 | `user-events` | Auth yaşam döngüsü için sözleşme | Gelecek kullanıcı read modeller |

## Ortak envelope

Her event aşağıdaki alanları taşır:

| Alan | Amaç |
|---|---|
| `EventId` | Global event kimliği ve Inbox idempotency anahtarı |
| `EventType` | Kararlı, semantik sözleşme adı |
| `Timestamp` | Olayın üretildiği UTC zaman |
| `Version` | `major.minor` şema sürümü |
| `CorrelationId` | Aynı kullanıcı/iş akışındaki olayları ilişkilendirme |
| `CausationId` | Bu olayı doğuran önceki event/komut kimliği |

## Uyumluluk kuralları

- Aynı major sürümde yeni alan yalnızca opsiyonel ve güvenli bir default ile eklenir.
- Alan adı/tipi/anlamı değiştirilmez ve zorunlu alan eklenmez.
- Breaking değişiklikte yeni major sözleşme paralel yayınlanır; consumer geçişi bitmeden eski sözleşme kaldırılmaz.
- Event type ve topic değişiklikleri ADR gerektirir.
- Yeni bir `BaseEvent` alt türü catalog'a eklenmezse contract testi başarısız olur.
