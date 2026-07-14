# ADR-0001: Mikroservis Mimarisini Koruma

- Durum: Kabul edildi
- Tarih: 14 Temmuz 2026
- Karar sahipleri: Proje sahibi ve teknik geliştirme süreci

## Bağlam

Vitrin yalnızca kısa sürede pazara çıkması hedeflenen bir MVP değildir. Projenin temel amaçlarından biri mikroservis mimarisini uygulamalı olarak öğrenmek, servisler arası iletişimi ve dağıtık sistem problemlerini çözmek ve bu yetkinlikleri CV/portföy üzerinde kanıtlanabilir biçimde göstermektir.

Mevcut çözüm Auth, Product, Voting, Comment, Notification, Analytics ve AI servisleri ile bir API Gateway ve Next.js frontend içermektedir. Servisleri modüler monolith altında birleştirmek operasyonel karmaşıklığı azaltabilir; ancak bu projenin dağıtık sistem öğrenme hedefini de daraltır.

## Karar

Mevcut mikroservis mimarisi ve servis sayısı korunacaktır. Aşama 0 kapsamında hiçbir servis birleştirilmeyecek veya kaldırılmayacaktır.

Servis sınırları yalnızca yeni bir ADR ile, ölçülebilir teknik gerekçelere dayanarak değiştirilebilir. “Klasör sayısını azaltmak” veya “daha çok teknoloji göstermek” tek başına sınır değişikliği gerekçesi değildir.

Hedef sorumluluklar:

| Bileşen | Temel veri ve sorumluluk sahipliği |
|---|---|
| Auth | Kimlik, kimlik doğrulama, yetkilendirme, kullanıcı profili ve roller |
| Product | Ürün kataloğu, topic, ürün yaşam döngüsü ve koleksiyonlar |
| Voting | Oyların tek yazma otoritesi ve oy invariant'ları |
| Comment | Yorumlar, cevaplar, düzenleme ve soft-delete kuralları |
| Notification | Bildirim inbox'ı, okunma durumu ve teslim kanalları |
| Analytics | Event ingestion, aggregate/read model ve analitik sorgular |
| AI | Analiz, etiketleme ve recommendation artefact'ları |
| Gateway | Edge routing, ortak güvenlik politikaları ve trafik kontrolü |
| Frontend | Kullanıcı deneyimi; hiçbir güvenlik kararının tek otoritesi değildir |

Voting servisinin tek yazma otoritesi olması hedeflenmiştir. Product servisindeki mevcut doğrudan vote yazma akışı, ilerleyen aşamada event-driven read model ile değiştirilecektir.

## Zorunlu kalite koşulları

Bir klasörün “mikroservis” olarak adlandırılması yeterli değildir. Her servis aşağıdaki koşulları sağlamalıdır:

- Açık bounded context ve veri sahipliği
- Başka servisin tablosuna veya DbContext'ine doğrudan erişmeme
- Bağımsız migration geçmişi
- Versiyonlanmış OpenAPI sözleşmesi
- Versiyonlanmış event sözleşmeleri ve event catalog
- Transactional Outbox ve idempotent Inbox/consumer
- Retry/backoff ve Dead Letter Queue politikası
- Integration ve consumer/producer contract testleri
- Bağımsız container build ve deployment tanımı
- Liveness/readiness health check'leri
- OpenTelemetry trace, metric ve yapılandırılmış log
- SLO, dashboard, runbook ve hata senaryosu
- Secret'ların environment/secret manager üzerinden yönetilmesi

## Sonuçlar

Olumlu sonuçlar:

- Mikroservis, Kafka, eventual consistency ve dağıtık tracing deneyimi gerçek problemler üzerinden gösterilebilir.
- Her servisin bağımsız test ve deployment yaşam döngüsü CV'de somut kanıt olarak sunulabilir.
- Veri sahipliği, Outbox/Inbox, idempotency ve contract evolution gibi ileri seviye konular proje kapsamına doğal olarak girer.

Olumsuz sonuçlar ve maliyetler:

- Local development ve CI daha fazla kaynak tüketir.
- Test süresi, deployment ve gözlemlenebilirlik yükü artar.
- Dağıtık transaction, eventual consistency ve hata kurtarma mekanizmaları zorunlu hâle gelir.
- Her servisin üretim kalitesine çıkarılması toplam geliştirme süresini uzatır.

Bu maliyetler projenin öğrenme ve CV hedefinin bilinçli bir parçası olarak kabul edilmiştir.

## Yeniden değerlendirme koşulları

Bu karar aşağıdaki durumlardan biri ölçülebilir biçimde ortaya çıkarsa yeniden değerlendirilebilir:

- İki servis arasında sürekli senkron ve atomik transaction zorunluluğu oluşması
- Bir servisin bağımsız veri veya deployment yaşam döngüsüne sahip olmaması
- Operasyonel maliyetin proje hedeflerini engellemesi
- Load/performance ölçümlerinin mevcut sınırın hatalı olduğunu göstermesi

Yeniden değerlendirme yeni bir ADR gerektirir; bu belge sessizce değiştirilmez.
