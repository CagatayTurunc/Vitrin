# 🔍 Vitrin Observability Stack - Kurulum Özeti

Bu dokümant Vitrin projesine eklenen observability özelliklerinin hızlı özetini içerir.

## ✅ Eklenen Özellikler

### 📊 Monitoring Stack
- **Jaeger**: Distributed tracing (Port 16686)
- **Prometheus**: Metrics collection (Port 9091)
- **Grafana**: Dashboards (Port 3004)
- **Elasticsearch**: Log storage (Port 9200)
- **Kibana**: Log analysis (Port 5601)

### 🏗️ Infrastructure Exporters  
- **PostgreSQL Exporter**: Database metrics (Port 9187)
- **Redis Exporter**: Cache metrics (Port 9121)
- **Kafka Exporter**: Message broker metrics (Port 9308)

### 🔧 Code-Level Features
- **Structured Logging** (Serilog)
- **Custom Business Metrics** (OpenTelemetry)
- **Request Correlation IDs**
- **Health Checks** (Database, Redis, Kafka)
- **Performance Monitoring**
- **Error Tracking & Categorization**

## 🚀 Hızlı Başlangıç

### 1. Environment Setup
```bash
# .env dosyasında gerekli değerler
GRAFANA_ADMIN_PASSWORD=your_password
LOG_LEVEL=Information
```

### 2. Servisleri Başlat
```powershell
# Tüm stack'i başlat
docker compose up -d

# Veya observability setup script'i kullan
.\scripts\setup-observability.ps1
```

### 3. Access URLs
- **Web App**: http://localhost:3003
- **API Gateway**: http://localhost:5000
- **Jaeger**: http://localhost:16686
- **Prometheus**: http://localhost:9091  
- **Grafana**: http://localhost:3004 (admin/[password])

## 📊 Pre-built Dashboards

### System Overview Dashboard
- Request rates ve response times
- Error rates ve status codes
- Active users count
- Cache hit/miss ratios
- Database query performance

### Business Metrics Dashboard  
- User registration trends
- Product submission rates
- Voting activity patterns
- Authentication success rates
- Comment engagement metrics

## 🔍 Key Metrics

### Business Metrics
```prometheus
vitrin_user_registrations_total{source="direct"}
vitrin_product_submissions_total{category="saas"}
vitrin_votes_total{vote_type="upvote"}
vitrin_auth_attempts_total{success="true"}
```

### Performance Metrics
```prometheus  
vitrin_request_duration_seconds
vitrin_database_query_duration_seconds
vitrin_cache_operation_duration_seconds
```

### System Metrics
```prometheus
vitrin_active_users
vitrin_cache_hits_total / vitrin_cache_misses_total  
vitrin_errors_total{error_type="validation"}
```

## 📝 Distributed Tracing

### Trace Flow
1. **Gateway** → JWT validation, routing decisions
2. **Auth Service** → User authentication, profile operations
3. **Business Services** → Product CRUD, voting, comments
4. **Event Bus** → Kafka async event publishing
5. **Database** → PostgreSQL/SQLite query execution

### Trace Attributes
- `service.name`, `service.version`
- `user.id`, `correlation.id`
- `http.method`, `http.status_code`
- `operation.type` (business, database, cache, etc.)

## 📋 Log Structure

Elasticsearch'te indexlenen log formatı:

```json
{
  "@timestamp": "2024-08-14T10:30:00.000Z",
  "level": "Information", 
  "messageTemplate": "Business operation: {Operation} executed by {UserId}",
  "properties": {
    "ServiceName": "Auth",
    "Operation": "UserRegistration",
    "UserId": "user123", 
    "CorrelationId": "abc123",
    "TraceId": "def456",
    "RequestPath": "/api/auth/register",
    "StatusCode": 200,
    "Duration": 1250
  }
}
```

## 🚨 Recommended Alerts

### Critical Thresholds
- **Error Rate**: > 5% for 5 minutes
- **Response Time**: P95 > 2 seconds
- **Cache Hit Rate**: < 80%
- **Database Connections**: > 80% pool usage
- **Service Health**: Health check failures

### Alert Channels
- Slack webhook notifications
- Email alerts
- PagerDuty integration (production)

## 🛠️ Troubleshooting

### Common Commands
```powershell
# Service durumu
docker compose ps

# Servis logları
docker compose logs vitrin-auth

# Metrics test
curl http://localhost:5000/metrics

# Health check
curl http://localhost:5000/health
```

### Debug Checklist
1. ✅ Docker containers running
2. ✅ Environment variables set
3. ✅ Network connectivity
4. ✅ Health checks passing
5. ✅ Metrics endpoint responding

## 📖 Detailed Documentation

Daha detaylı bilgi için:
- [Main README](README.md) - Full project documentation
- [Observability Guide](observability/README.md) - Comprehensive monitoring setup
- [Grafana Dashboards](observability/grafana/dashboards/) - Dashboard configurations

## 🎯 Production Recommendations

### Scaling
- Prometheus HA setup with remote storage
- Grafana load balancer setup
- Elasticsearch cluster (minimum 3 nodes)

### Retention Policies  
- **Metrics**: 30 days
- **Traces**: 7 days  
- **Logs**: 90 days

### Security
- Grafana authentication (OAuth/LDAP)
- Elasticsearch security enabled
- Network policies for internal services