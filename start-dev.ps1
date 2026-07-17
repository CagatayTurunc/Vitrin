# Vitrin local development launcher
# Docker: PostgreSQL, Redis and Kafka only
# Apps: dotnet watch and Next.js on the host

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

function Test-PortInUse {
    param([int]$Port)

    return $null -ne (Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue |
        Select-Object -First 1)
}

Write-Host ""
Write-Host "Vitrin local gelistirme ortami baslatiliyor..." -ForegroundColor Cyan

# Docker tarafinda eski uygulama image'lari calismasin; yalnizca altyapi kalir.
$appContainers = @(
    "vitrin-product",
    "vitrin-notification",
    "vitrin-auth",
    "vitrin-voting",
    "vitrin-comment",
    "vitrin-analytics",
    "vitrin-ai",
    "vitrin-gateway",
    "vitrin-web"
)

Write-Host "[1/4] Docker uygulama container'lari durduruluyor..." -ForegroundColor Yellow
foreach ($container in $appContainers) {
    docker stop $container 2>$null | Out-Null
}

Write-Host "[2/4] PostgreSQL, Redis ve Kafka baslatiliyor..." -ForegroundColor Yellow
docker compose -f "$root\docker-compose.infra.yml" up -d
if ($LASTEXITCODE -ne 0) {
    throw "Docker altyapisi baslatilamadi. Docker Desktop'in acik oldugunu kontrol et."
}

# Kafka hazirsa hemen devam et; en fazla 30 saniye bekle.
$kafkaReady = $false
for ($attempt = 1; $attempt -le 15; $attempt++) {
    docker exec vitrin-kafka kafka-topics --bootstrap-server localhost:9092 --list 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        $kafkaReady = $true
        break
    }
    Start-Sleep -Seconds 2
}

if (-not $kafkaReady) {
    throw "Kafka 30 saniye icinde hazir olmadi. vitrin-kafka loglarini kontrol et."
}

$kafkaTopics = @(
    "notification-events",
    "voting-events",
    "analytics-events",
    "social-events",
    "user-events"
)
foreach ($topic in $kafkaTopics) {
    docker exec vitrin-kafka kafka-topics `
        --bootstrap-server localhost:9092 `
        --create `
        --if-not-exists `
        --topic $topic `
        --partitions 1 `
        --replication-factor 1 2>$null | Out-Null
}

$services = @(
    @{ Name = "Auth";         Path = "$root\src\Services\Auth\Vitrin.Auth.Api";                 Port = 5104; HasDatabase = $true },
    @{ Name = "Product";      Path = "$root\src\Services\Product\Vitrin.Product.Api";           Port = 5177; HasDatabase = $true },
    @{ Name = "Voting";       Path = "$root\src\Services\Voting\Vitrin.Voting.Api";             Port = 5143; HasDatabase = $true },
    @{ Name = "Comment";      Path = "$root\src\Services\Comment\Vitrin.Comment.Api";           Port = 5100; HasDatabase = $true },
    @{ Name = "Notification"; Path = "$root\src\Services\Notification\Vitrin.Notification.Api"; Port = 5101; HasDatabase = $true },
    @{ Name = "Analytics";    Path = "$root\src\Services\Analytics\Vitrin.Analytics.Api";       Port = 5102; HasDatabase = $true },
    @{ Name = "AI";           Path = "$root\src\Services\Ai\Vitrin.Ai.Api";                     Port = 5103; HasDatabase = $true },
    @{ Name = "Gateway";      Path = "$root\src\Gateways\Vitrin.Gateway";                       Port = 5000; HasDatabase = $false }
)

Write-Host "[3/4] Bekleyen veritabani migration'lari kontrol ediliyor..." -ForegroundColor Yellow
foreach ($service in $services | Where-Object { $_.HasDatabase -and -not (Test-PortInUse $_.Port) }) {
    Write-Host "  - $($service.Name)" -ForegroundColor DarkGray
    Push-Location $service.Path
    try {
        $env:Logging__EventLog__LogLevel__Default = "None"
        dotnet run --no-restore -- --migrate-only
        if ($LASTEXITCODE -ne 0) {
            throw "$($service.Name) migration'i uygulanamadi."
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host "[4/4] Servisler baslatiliyor..." -ForegroundColor Yellow
foreach ($service in $services) {
    if (Test-PortInUse $service.Port) {
        Write-Host "  = $($service.Name) zaten calisiyor: http://localhost:$($service.Port)" -ForegroundColor DarkYellow
        continue
    }

    Write-Host "  + $($service.Name): http://localhost:$($service.Port)" -ForegroundColor Gray
    $command = "cd '$($service.Path)'; " +
        "`$env:Logging__EventLog__LogLevel__Default = 'None'; " +
        "`$host.UI.RawUI.WindowTitle = 'Vitrin - $($service.Name)'; " +
        "dotnet watch run --no-restore --urls http://localhost:$($service.Port)"

    Start-Process powershell `
        -ArgumentList @("-NoExit", "-Command", $command) `
        -WindowStyle Normal
}

$webPort = 3001
$webPath = "$root\src\Web\Vitrin.Web.UI"
if (Test-PortInUse $webPort) {
    Write-Host "  = Next.js zaten calisiyor: http://localhost:$webPort" -ForegroundColor DarkYellow
}
else {
    Write-Host "  + Next.js: http://localhost:$webPort" -ForegroundColor Gray
    $webCommand = "cd '$webPath'; " +
        "`$host.UI.RawUI.WindowTitle = 'Vitrin - Next.js'; " +
        "npm run dev -- --port $webPort"

    Start-Process powershell `
        -ArgumentList @("-NoExit", "-Command", $webCommand) `
        -WindowStyle Normal
}

Write-Host ""
Write-Host "Hazir: http://localhost:3001" -ForegroundColor Green
Write-Host "Kod degisiklikleri dotnet watch ve Next.js hot reload ile otomatik uygulanir." -ForegroundColor Green
Write-Host "Docker image rebuild gerekmez." -ForegroundColor Green
