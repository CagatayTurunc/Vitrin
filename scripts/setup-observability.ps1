# Vitrin Observability Setup Script
# Bu script observability stack'ini kolayca kurmanızı sağlar

param(
    [switch]$SkipBuild,
    [switch]$OnlyObservability,
    [string]$LogLevel = "Information"
)

Write-Host "🔍 Vitrin Observability Stack Setup" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Check prerequisites
if (!(Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker is required but not installed."
    exit 1
}

if (!(Get-Command docker-compose -ErrorAction SilentlyContinue)) {
    Write-Error "Docker Compose is required but not installed."
    exit 1
}

# Check if .env file exists
if (!(Test-Path ".env")) {
    Write-Host "⚠️  .env file not found. Creating from template..." -ForegroundColor Yellow
    if (Test-Path ".env.example") {
        Copy-Item ".env.example" ".env"
        Write-Host "✅ .env file created. Please configure your settings." -ForegroundColor Green
        Write-Host "   Required: POSTGRES_PASSWORD, JWT_SECRET, NEXTAUTH_SECRET, GRAFANA_ADMIN_PASSWORD" -ForegroundColor Yellow
    } else {
        Write-Error ".env.example file not found. Cannot create .env file."
        exit 1
    }
}

# Verify critical environment variables
$envContent = Get-Content ".env" -Raw
$requiredVars = @("POSTGRES_PASSWORD", "JWT_SECRET", "NEXTAUTH_SECRET", "GRAFANA_ADMIN_PASSWORD")
$missingVars = @()

foreach ($var in $requiredVars) {
    if ($envContent -notmatch "$var=.+") {
        $missingVars += $var
    }
}

if ($missingVars.Count -gt 0) {
    Write-Error "Missing required environment variables: $($missingVars -join ', ')"
    Write-Host "Please configure these in your .env file before proceeding." -ForegroundColor Yellow
    exit 1
}

# Create required directories
$directories = @(
    "logs",
    "data/voting", 
    "data/notification",
    "data/analytics", 
    "data/ai",
    "observability/prometheus",
    "observability/grafana/provisioning/datasources",
    "observability/grafana/provisioning/dashboards", 
    "observability/grafana/dashboards"
)

foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "📁 Created directory: $dir" -ForegroundColor Green
    }
}

# Stop existing containers
Write-Host "🛑 Stopping existing containers..." -ForegroundColor Yellow
docker-compose down --remove-orphans

if (!$OnlyObservability -and !$SkipBuild) {
    Write-Host "🏗️  Building application images..." -ForegroundColor Cyan
    docker-compose build
}

# Start infrastructure services first
Write-Host "🚀 Starting infrastructure services..." -ForegroundColor Cyan
docker-compose up -d postgres redis zookeeper kafka

# Wait for infrastructure
Write-Host "⏳ Waiting for infrastructure to be ready..." -ForegroundColor Yellow
Start-Sleep 30

# Start observability stack
Write-Host "📊 Starting observability stack..." -ForegroundColor Cyan
docker-compose up -d jaeger prometheus grafana elasticsearch kibana postgres-exporter redis-exporter kafka-exporter

# Wait for observability services
Write-Host "⏳ Waiting for observability services..." -ForegroundColor Yellow
Start-Sleep 45

if (!$OnlyObservability) {
    # Run migrations
    Write-Host "🗄️  Running database migrations..." -ForegroundColor Cyan
    try {
        & ".\scripts\run-migrations.ps1"
    } catch {
        Write-Warning "Migration script not found or failed. You may need to run migrations manually."
    }

    # Start application services
    Write-Host "🎯 Starting application services..." -ForegroundColor Cyan
    docker-compose up -d vitrin-auth vitrin-product vitrin-voting vitrin-comment vitrin-notification vitrin-analytics vitrin-ai

    # Start gateway and web
    Write-Host "🌐 Starting gateway and web..." -ForegroundColor Cyan
    docker-compose up -d vitrin-gateway vitrin-web
}

# Health check
Write-Host "🏥 Checking service health..." -ForegroundColor Cyan
Start-Sleep 30

$services = docker-compose ps --services
$healthyServices = 0
$totalServices = 0

foreach ($service in $services) {
    $totalServices++
    $status = docker-compose ps $service --format "{{.State}}"
    if ($status -eq "running") {
        $healthyServices++
        Write-Host "✅ $service: Running" -ForegroundColor Green
    } else {
        Write-Host "❌ $service: $status" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "📈 Service Status: $healthyServices/$totalServices running" -ForegroundColor $(if ($healthyServices -eq $totalServices) { "Green" } else { "Yellow" })

# Display access information
Write-Host ""
Write-Host "🎉 Setup Complete! Access your services:" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host "🔍 Grafana Dashboard:    http://localhost:3001" -ForegroundColor Cyan
Write-Host "📊 Prometheus:           http://localhost:9090" -ForegroundColor Cyan  
Write-Host "🔗 Jaeger Tracing:       http://localhost:16686" -ForegroundColor Cyan
Write-Host "📋 Kibana Logs:          http://localhost:5601" -ForegroundColor Cyan
Write-Host "🌐 Vitrin Web:           http://localhost:3002" -ForegroundColor Cyan
Write-Host "🔌 API Gateway:          http://localhost:5000" -ForegroundColor Cyan
Write-Host ""

# Show credentials
$grafanaPassword = (Select-String -Path ".env" -Pattern "GRAFANA_ADMIN_PASSWORD=(.+)").Matches[0].Groups[1].Value
Write-Host "🔑 Grafana Credentials:" -ForegroundColor Yellow
Write-Host "   Username: admin" -ForegroundColor White
Write-Host "   Password: $grafanaPassword" -ForegroundColor White
Write-Host ""

# Show useful commands
Write-Host "🛠️  Useful Commands:" -ForegroundColor Blue
Write-Host "   View all logs:           docker-compose logs -f" -ForegroundColor White
Write-Host "   View specific service:   docker-compose logs -f vitrin-auth" -ForegroundColor White
Write-Host "   Restart service:         docker-compose restart vitrin-auth" -ForegroundColor White
Write-Host "   Check metrics:           curl http://localhost:5000/metrics" -ForegroundColor White
Write-Host "   Health check:            curl http://localhost:5000/health" -ForegroundColor White
Write-Host ""

Write-Host "🎯 Next Steps:" -ForegroundColor Magenta
Write-Host "   1. Visit Grafana and explore the pre-built dashboards" -ForegroundColor White
Write-Host "   2. Check Jaeger for distributed traces" -ForegroundColor White  
Write-Host "   3. Monitor application logs in Kibana" -ForegroundColor White
Write-Host "   4. Set up alerts based on your requirements" -ForegroundColor White
Write-Host ""

if ($healthyServices -lt $totalServices) {
    Write-Host "⚠️  Some services are not running. Check logs with:" -ForegroundColor Yellow
    Write-Host "   docker-compose logs [service-name]" -ForegroundColor White
}