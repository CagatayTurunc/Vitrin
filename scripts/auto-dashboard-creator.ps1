# Automatic Grafana Dashboard Creator
# Bu script Grafana API kullanarak otomatik dashboard oluşturur

Write-Host "🚀 Grafana Dashboard Otomatik Oluşturucu" -ForegroundColor Green
Write-Host ""

# Grafana bilgileri
$grafanaUrl = "http://localhost:3004"
$username = "admin"

# Kullanıcıdan şifre iste
Write-Host "Grafana admin şifresini girin:" -ForegroundColor Yellow
$password = Read-Host -AsSecureString
$passwordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))

# Base64 encode credentials
$auth = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("${username}:${passwordPlain}"))
$headers = @{
    'Content-Type' = 'application/json'
    'Authorization' = "Basic $auth"
}

# Test connection
try {
    Write-Host "🔌 Grafana bağlantısı test ediliyor..." -ForegroundColor Yellow
    $orgResponse = Invoke-RestMethod -Uri "$grafanaUrl/api/org" -Headers $headers
    Write-Host "✅ Bağlantı başarılı! Org: $($orgResponse.name)" -ForegroundColor Green
} catch {
    Write-Host "❌ Grafana'ya bağlanılamadı: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "🔧 Çözüm önerileri:" -ForegroundColor Yellow
    Write-Host "1. Grafana'nın çalıştığından emin olun: http://localhost:3004" -ForegroundColor White
    Write-Host "2. Kullanıcı adı/şifre doğru mu kontrol edin" -ForegroundColor White
    Write-Host "3. Tarayıcıda giriş yapmayı deneyin" -ForegroundColor White
    exit 1
}

# Data source kontrol et
Write-Host "🔍 Prometheus data source kontrol ediliyor..." -ForegroundColor Yellow
try {
    $datasources = Invoke-RestMethod -Uri "$grafanaUrl/api/datasources" -Headers $headers
    $prometheusDs = $datasources | Where-Object { $_.type -eq "prometheus" } | Select-Object -First 1
    
    if ($prometheusDs) {
        Write-Host "✅ Prometheus data source bulundu: $($prometheusDs.name)" -ForegroundColor Green
        $datasourceUid = $prometheusDs.uid
    } else {
        Write-Host "❌ Prometheus data source bulunamadı!" -ForegroundColor Red
        Write-Host "🔧 Manuel olarak Prometheus data source ekleyin:" -ForegroundColor Yellow
        Write-Host "- URL: http://localhost:9091" -ForegroundColor White
        exit 1
    }
} catch {
    Write-Host "❌ Data source kontrol edilemedi: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Dashboard JSON oluştur
$dashboardJson = @{
    dashboard = @{
        id = $null
        title = "🚀 Vitrin Production Dashboard"
        tags = @("vitrin", "monitoring")
        timezone = "browser"
        panels = @(
            @{
                id = 1
                title = "🚦 Service Status"
                type = "stat"
                gridPos = @{ h = 6; w = 24; x = 0; y = 0 }
                fieldConfig = @{
                    defaults = @{
                        mappings = @(
                            @{
                                options = @{
                                    "0" = @{ text = "DOWN"; color = "red" }
                                    "1" = @{ text = "UP"; color = "green" }
                                }
                                type = "value"
                            }
                        )
                        color = @{ mode = "thresholds" }
                        thresholds = @{
                            steps = @(
                                @{ color = "red"; value = $null }
                                @{ color = "green"; value = 1 }
                            )
                        }
                    }
                }
                options = @{
                    colorMode = "background"
                    graphMode = "none"
                    justifyMode = "center"
                }
                targets = @(
                    @{
                        expr = 'up{job=~"vitrin.*"}'
                        legendFormat = '{{job}}'
                        refId = "A"
                    }
                )
                datasource = @{ type = "prometheus"; uid = $datasourceUid }
            },
            @{
                id = 2
                title = "🔄 CPU Usage"
                type = "timeseries"
                gridPos = @{ h = 8; w = 12; x = 0; y = 6 }
                fieldConfig = @{
                    defaults = @{
                        unit = "percent"
                        max = 100
                        min = 0
                        color = @{ mode = "palette-classic" }
                    }
                }
                targets = @(
                    @{
                        expr = 'rate(process_cpu_seconds_total{job=~"vitrin.*"}[5m]) * 100'
                        legendFormat = '{{job}} CPU'
                        refId = "A"
                    }
                )
                datasource = @{ type = "prometheus"; uid = $datasourceUid }
            },
            @{
                id = 3
                title = "💾 Memory Usage"
                type = "timeseries"
                gridPos = @{ h = 8; w = 12; x = 12; y = 6 }
                fieldConfig = @{
                    defaults = @{
                        unit = "bytes"
                        color = @{ mode = "palette-classic" }
                    }
                }
                targets = @(
                    @{
                        expr = 'process_resident_memory_bytes{job=~"vitrin.*"}'
                        legendFormat = '{{job}} Memory'
                        refId = "A"
                    }
                )
                datasource = @{ type = "prometheus"; uid = $datasourceUid }
            },
            @{
                id = 4
                title = "📈 Redis Operations"
                type = "timeseries"
                gridPos = @{ h = 8; w = 12; x = 0; y = 14 }
                fieldConfig = @{
                    defaults = @{
                        unit = "ops"
                        color = @{ mode = "palette-classic" }
                    }
                }
                targets = @(
                    @{
                        expr = 'rate(redis_commands_processed_total[5m])'
                        legendFormat = 'Redis Commands/sec'
                        refId = "A"
                    }
                )
                datasource = @{ type = "prometheus"; uid = $datasourceUid }
            },
            @{
                id = 5
                title = "🎯 Cache Hit Rate"
                type = "timeseries"
                gridPos = @{ h = 8; w = 12; x = 12; y = 14 }
                fieldConfig = @{
                    defaults = @{
                        unit = "percent"
                        max = 100
                        min = 0
                        color = @{ mode = "palette-classic" }
                    }
                }
                targets = @(
                    @{
                        expr = 'redis_keyspace_hits_total / (redis_keyspace_hits_total + redis_keyspace_misses_total) * 100'
                        legendFormat = 'Cache Hit Rate %'
                        refId = "A"
                    }
                )
                datasource = @{ type = "prometheus"; uid = $datasourceUid }
            }
        )
        time = @{ from = "now-1h"; to = "now" }
        refresh = "30s"
        schemaVersion = 39
        version = 1
        uid = "vitrin-auto-dashboard"
    }
    overwrite = $true
} | ConvertTo-Json -Depth 20

# Dashboard oluştur
Write-Host "📊 Dashboard oluşturuluyor..." -ForegroundColor Yellow
try {
    $createResponse = Invoke-RestMethod -Uri "$grafanaUrl/api/dashboards/db" -Method POST -Headers $headers -Body $dashboardJson
    Write-Host "✅ Dashboard başarıyla oluşturuldu!" -ForegroundColor Green
    Write-Host "🔗 Dashboard URL: $grafanaUrl$($createResponse.url)" -ForegroundColor Cyan
    
    # Tarayıcıda aç
    Start-Process "$grafanaUrl$($createResponse.url)"
    
} catch {
    Write-Host "❌ Dashboard oluşturulamadı: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎉 İşlem tamamlandı!" -ForegroundColor Green