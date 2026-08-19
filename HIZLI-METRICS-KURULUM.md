# ⚡ Hızlı Metrics Kurulumu

Mikroservislerinize HTTP metrics eklemek için:

## 1. Her .NET Projesine NuGet Paketi Ekle

```bash
# Her mikroservis klasöründe çalıştır
dotnet add package prometheus-net.AspNetCore
```

## 2. Program.cs'e Ekle

Her mikroservisin `Program.cs` dosyasına şunu ekle:

```csharp
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Mevcut servisleriniz...
builder.Services.AddControllers();

var app = builder.Build();

// Prometheus middleware ekle
app.UseRouting();
app.UseHttpMetrics(); // ⭐ Bu satır HTTP metriklerini otomatik toplar

app.MapControllers();
app.MapMetrics(); // ⭐ /metrics endpoint'ini expose eder

app.Run();
```

## 3. Test Et

Her mikroserviste bu URL'yi kontrol et:
- `http://localhost:PORT/metrics`

Bu URL'de Prometheus formatında metrikler görmelisin.

## 4. Dashboard'da Sonuçları Gör

Grafana dashboard'unda artık:
- ✅ HTTP request rate
- ✅ HTTP response times  
- ✅ Error rates
- ✅ CPU/Memory usage

metriklerini görebilirsin!

## Hangi Projeler?

Bu değişiklikleri şu projelerde yap:
- `Vitrin.Auth.Api`
- `Vitrin.Product.Api`  
- `Vitrin.Comment.Api`
- `Vitrin.Voting.Api`
- `Vitrin.Analytics.Api`
- `Vitrin.Notification.Api`
- `Vitrin.Ai.Api`
- `Vitrin.Gateway`

## Test Komutu

```bash
# Tüm servisleri test et
curl http://localhost:5000/metrics  # Gateway
curl http://localhost:5177/metrics  # Product
# vs...
```