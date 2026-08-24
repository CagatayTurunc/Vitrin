# .NET Core Projede Prometheus Metrics Kurulumu

## 1. NuGet Paketlerini Yükle

```bash
dotnet add package prometheus-net
dotnet add package prometheus-net.AspNetCore
```

## 2. Program.cs veya Startup.cs'e Ekle

```csharp
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Diğer servisleriniz...
builder.Services.AddControllers();

var app = builder.Build();

// Prometheus middleware ekle
app.UseRouting();
app.UseHttpMetrics(); // HTTP metriklerini otomatik toplar

app.MapControllers();

// Metrics endpoint'i expose et
app.MapMetrics(); // /metrics endpoint'i oluşturur

app.Run();
```

## 3. Custom Business Metrics Ekle

```csharp
using Prometheus;

public class BusinessMetrics
{
    // Counter'lar (sürekli artan değerler)
    public static readonly Counter UserRegistrations = Metrics
        .CreateCounter("vitrin_user_registrations_total", 
                      "Total number of user registrations",
                      new[] { "environment" });
    
    public static readonly Counter Votes = Metrics
        .CreateCounter("vitrin_votes_total", 
                      "Total number of votes",
                      new[] { "environment" });
    
    public static readonly Counter Comments = Metrics
        .CreateCounter("vitrin_comments_total", 
                      "Total number of comments", 
                      new[] { "environment" });
    
    public static readonly Counter ProductSubmissions = Metrics
        .CreateCounter("vitrin_product_submissions_total", 
                      "Total number of product submissions",
                      new[] { "environment" });
    
    public static readonly Counter Revenue = Metrics
        .CreateCounter("vitrin_revenue_total", 
                      "Total revenue generated",
                      new[] { "environment" });
    
    public static readonly Counter Purchases = Metrics
        .CreateCounter("vitrin_purchases_total", 
                      "Total number of purchases",
                      new[] { "environment" });
    
    public static readonly Counter Visits = Metrics
        .CreateCounter("vitrin_visits_total", 
                      "Total page visits",
                      new[] { "environment" });
    
    // Deployment info (version tracking)
    public static readonly Gauge DeploymentInfo = Metrics
        .CreateGauge("vitrin_deployment_info", 
                    "Deployment information",
                    new[] { "version", "environment" });
}
```

## 4. Controller'larda Metrics Kullan

```csharp
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegisterDto dto)
    {
        // Registration logic...
        
        // Metric'i artır
        BusinessMetrics.UserRegistrations
            .WithLabels(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "development")
            .Inc();
        
        return Ok();
    }
    
    [HttpPost("{id}/vote")]
    public async Task<IActionResult> Vote(int id)
    {
        // Voting logic...
        
        BusinessMetrics.Votes
            .WithLabels(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "development")
            .Inc();
        
        return Ok();
    }
}
```

## 5. Environment Labels Ayarla

### appsettings.json
```json
{
  "Metrics": {
    "Environment": "production"
  }
}
```

### Docker Compose ile Environment
```yaml
version: '3.8'
services:
  vitrin-web:
    image: vitrin:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=production
    ports:
      - "80:80"
    labels:
      - "prometheus.io/scrape=true"
      - "prometheus.io/port=80"
      - "prometheus.io/path=/metrics"
```

## 6. Deployment Sırasında Version Metric'i Set Et

```csharp
// Program.cs'de startup sırasında
BusinessMetrics.DeploymentInfo
    .WithLabels("v1.2.3", "production")
    .Set(1);
```