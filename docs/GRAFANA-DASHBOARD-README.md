# 🚀 Vitrin Production Dashboard - Profesyonel Monitoring Sistemi

Bu klasörde, Vitrin projesi için profesyonel bir Grafana dashboard'u ve alerting sistemi bulunmaktadır. Dashboard, endüstri standardı **RED** ve **USE** metodolojilerini takip eder.

## 📁 Dosya Açıklamaları

- **`vitrin-production-dashboard-v2.json`**: Ana production dashboard (ÖNERİLEN)
- **`vitrin-production-dashboard.json`**: Eski versiyon dashboard
- **`vitrin-minimal-test.json`**: Test amaçlı minimal dashboard
- **`vitrin-alerting-rules.yml`**: Prometheus alerting kuralları
- **`GRAFANA-DASHBOARD-README.md`**: Bu dosya (kurulum rehberi)

## 🔥 Dashboard Özellikleri

### Golden Signals (RED Methodology)
- **📊 Request Rate (RPS)**: Saniyede gelen istek sayısı
- **❌ Error Rate (%)**: 4xx/5xx hata oranları  
- **⚡ Response Time**: P95 ve P99 gecikme metrikleri

### Infrastructure (USE Methodology)  
- **🔄 CPU Usage**: İşlemci kullanım oranları
- **💾 Memory Usage**: Bellek tüketimi
- **🗑️ .NET GC Duration**: Çöp toplayıcı performansı
- **🧵 Active Threads**: Aktif thread sayıları

### Dependencies & External Services
- **🗄️ PostgreSQL Connections**: Veritabanı bağlantı havuzu
- **📈 Redis Performance**: Cache hit oranları ve operasyon hızı
- **📤 Message Queue Status**: RabbitMQ/Kafka kuyruk durumu

### Business Metrics
- **🎯 User Engagement**: Kayıt, oy, yorum metrikleri
- **💰 Revenue & Conversion**: Gelir ve dönüşüm oranları

## 🛠️ Kurulum Adımları

### 1. Grafana Dashboard Import Etme

1. Grafana web arayüzüne giriş yapın
2. Sol menüden **"+"** → **"Import"** seçin
3. **"Upload JSON file"** butonuna tıklayın
4. `vitrin-production-dashboard-v2.json` dosyasını seçin
5. **"Import"** butonuna tıklayın

### 2. Variables (Değişkenler) Kurulumu

Dashboard otomatik olarak şu değişkenleri oluşturacak:
- **Environment**: `production`, `staging`, `development`
- **Service**: Tüm mikroservislerin listesi

### 3. Prometheus Alerting Rules Kurulumu

```bash
# Prometheus config dizinine kopyalayın
cp vitrin-alerting-rules.yml /etc/prometheus/rules/

# prometheus.yml içine ekleyin:
rule_files:
  - "rules/vitrin-alerting-rules.yml"

# Prometheus'u yeniden başlatın
systemctl restart prometheus
```

### 4. Gerekli Prometheus Metrikleri

Dashboard'un düzgün çalışması için şu metriklerin expose edilmesi gerekir:

```promql
# HTTP Metrikleri
http_requests_total{service, status, method, environment}
http_request_duration_seconds_bucket{service, environment}

# System Metrikleri  
up{service, environment}
process_cpu_seconds_total{service, environment}
process_resident_memory_bytes{service, environment}
process_threads{service, environment}

# .NET Metrikleri
dotnet_gc_collection_seconds_total{service, environment, generation}

# PostgreSQL Metrikleri
pg_stat_database_numbackends{datname, environment}
pg_settings_max_connections{environment}

# Redis Metrikleri
redis_commands_processed_total{environment}
redis_keyspace_hits_total{environment}
redis_keyspace_misses_total{environment}

# RabbitMQ Metrikleri
rabbitmq_queue_messages_ready{queue, environment}
rabbitmq_queue_messages_unacknowledged{queue, environment}

# Business Metrikleri
vitrin_user_registrations_total{environment}
vitrin_votes_total{environment}
vitrin_comments_total{environment}
vitrin_product_submissions_total{environment}
vitrin_revenue_total{environment}
vitrin_purchases_total{environment}
vitrin_visits_total{environment}
vitrin_deployment_info{version, environment}
```

## 🎨 Dashboard Özelleştirme

### Threshold (Eşik) Değerlerini Değiştirme

```json
"thresholds": {
  "steps": [
    {"color": "green", "value": null},
    {"color": "yellow", "value": 70},    // Sarı eşik
    {"color": "red", "value": 85}        // Kırmızı eşik
  ]
}
```

### Yeni Panel Ekleme

1. Dashboard'da **"Add panel"** butonuna tıklayın
2. Prometheus query'sini yazın
3. Görselleştirme tipini seçin (timeseries, stat, gauge, etc.)
4. Threshold değerlerini ayarlayın
5. **"Apply"** butonuna tıklayın

### Annotation (Deployment Gösterimi)

Dashboard otomatik olarak deployment'ları gösterecektir. Bunun için:

```promql
# Bu metriği deployment sırasında expose edin
vitrin_deployment_info{version="v1.2.3", environment="production"}
```

## 📊 Alert Manager Entegrasyonu

Alerting kuralları şu severity seviyeleri kullanır:

- **🔴 Critical**: Anında müdahale gereken durumlar
- **🟡 Warning**: İzlenmesi gereken durumlar  
- **🔵 Info**: Bilgilendirme amaçlı

### Slack Entegrasyonu Örneği

```yaml
route:
  group_by: ['alertname']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 1h
  receiver: 'web.hook'

receivers:
- name: 'web.hook'
  slack_configs:
  - api_url: 'YOUR_SLACK_WEBHOOK_URL'
    channel: '#alerts'
    title: '🚨 Vitrin Production Alert'
    text: '{{ range .Alerts }}{{ .Annotations.description }}{{ end }}'
```

## 🔍 Monitoring Best Practices

### 1. Alert Fatigue'dan Kaçının
- Sadece **actionable** alertler oluşturun
- Threshold değerlerini gerçekçi tutun
- Alert'leri kategorize edin (critical, warning, info)

### 2. Dashboard Organizasyonu
- En önemli metrikleri üste koyun
- Benzer metrikleri gruplayın
- Row'lar kullanarak kategorize edin

### 3. Business Metrics'i Dahil Edin
- Technical metriklerin yanında business KPI'larını da izleyin
- Revenue, user engagement, conversion rates

### 4. Capacity Planning
- Resource kullanım trendlerini takip edin
- Growth projeksiyonları yapın
- Scalability planlaması

## 🔧 Troubleshooting

### Dashboard Görünmüyor?
- Prometheus veri source'unun doğru olduğundan emin olun
- Metrics'lerin expose edildiğini kontrol edin
- Time range'i kontrol edin

### Alertler Çalışmıyor?
- Prometheus rules dosyasının yüklendiğini kontrol edin
- Alert Manager konfigürasyonunu kontrol edin
- Notification channel'larını test edin

### Performans Sorunları?
- Query'leri optimize edin
- Time range'i kısaltın
- Dashboard'ı daha az panel ile test edin

## 📈 Gelecek Geliştirmeler

- [ ] Custom business metrics dashboard'u
- [ ] Cost monitoring dashboard'u  
- [ ] Security monitoring integration
- [ ] Mobile-friendly responsive design
- [ ] Dark/Light theme toggle

---

**💡 Not**: Bu dashboard, SRE (Site Reliability Engineering) best practice'lerini takip eder ve production ortamında güvenle kullanılabilir. Sorularınız için Grafana dokümantasyonuna veya team lead'e başvurabilirsiniz.