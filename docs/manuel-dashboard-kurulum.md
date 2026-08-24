# 📊 Manuel Dashboard Oluşturma Rehberi

JSON import çalışmadığı için manuel olarak dashboard oluşturalım.

## 1️⃣ Grafana'ya Giriş Yap

1. Tarayıcıda **http://localhost:3004** aç
2. Giriş yap (admin/admin veya admin/admin123 dene)

## 2️⃣ Yeni Dashboard Oluştur

1. Sol menüden **"+"** → **"Dashboard"** 
2. **"Add visualization"** butonuna tıkla

## 3️⃣ İlk Panel: Service Status

**Panel Ayarları:**
- **Query:** `up{job=~"vitrin.*"}`
- **Legend:** `{{job}}`
- **Panel Type:** Stat
- **Title:** 🚦 Service Status

**Threshold Ayarları:**
- 0 = Red (DOWN)  
- 1 = Green (UP)

**Value Mappings:**
- 0 → "DOWN"
- 1 → "UP"

## 4️⃣ İkinci Panel: CPU Usage

**Panel Ayarları:**
- **Query:** `rate(process_cpu_seconds_total{job=~"vitrin.*"}[5m]) * 100`
- **Legend:** `{{job}} CPU`
- **Panel Type:** Time series
- **Title:** 🔄 CPU Usage
- **Unit:** Percent (0-100)
- **Y-Axis Max:** 100

## 5️⃣ Üçüncü Panel: Memory Usage

**Panel Ayarları:**
- **Query:** `process_resident_memory_bytes{job=~"vitrin.*"}`
- **Legend:** `{{job}} Memory`
- **Panel Type:** Time series  
- **Title:** 💾 Memory Usage
- **Unit:** Bytes

## 6️⃣ Dördüncü Panel: Redis Operations

**Panel Ayarları:**
- **Query:** `rate(redis_commands_processed_total[5m])`
- **Legend:** `Redis Commands/sec`
- **Panel Type:** Time series
- **Title:** 📈 Redis Operations
- **Unit:** ops

## 7️⃣ Beşinci Panel: Redis Cache Hit Rate

**Panel Ayarları:**
- **Query:** `redis_keyspace_hits_total / (redis_keyspace_hits_total + redis_keyspace_misses_total) * 100`
- **Legend:** `Cache Hit Rate %`
- **Panel Type:** Time series
- **Title:** 🎯 Cache Hit Rate
- **Unit:** Percent (0-100)

## 8️⃣ Altıncı Panel: PostgreSQL Connections

**Panel Ayarları:**
- **Query:** `pg_up`
- **Legend:** `PostgreSQL Status`
- **Panel Type:** Stat
- **Title:** 🗄️ PostgreSQL Status

## 9️⃣ Dashboard Ayarları

**Sol üst köşeden:**
- **Time Range:** Last 1 hour
- **Refresh:** 30s
- **Dashboard Title:** "Vitrin Monitoring"

## 🔟 Dashboard'u Kaydet

1. Sağ üstten **"Save"** butonuna tıkla
2. İsim: "Vitrin Production Dashboard"
3. **"Save"** butonuna tıkla

## ✅ Sonuç

Artık profesyonel bir monitoring dashboard'unuz var!

## 🚨 Sorun Giderme

**Panel'de "No data" yazıyorsa:**
1. Query'i manuel test et: Prometheus'da (http://localhost:9091) aynı query'i çalıştır
2. Time range'i artır (Last 6 hours)
3. Data source'un doğru seçildiğini kontrol et