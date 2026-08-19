# Docker Registry Sorunu Çözücü
# Türkiye'deki Docker Hub erişim sorunlarını çözer

Write-Host "Docker Registry Sorunu Çözülüyor..." -ForegroundColor Cyan
Write-Host ""

# Docker'ı durdur
Write-Host "1. Docker servislerini durduruluyor..." -ForegroundColor Yellow
try {
    Stop-Process -Name "Docker Desktop" -Force -ErrorAction SilentlyContinue
    Stop-Process -Name "com.docker.backend" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 5
} catch {
    Write-Host "Docker processleri zaten durdurulmuş." -ForegroundColor Green
}

# Docker daemon.json dosyasını oluştur/güncelle
$dockerConfigPath = "$env:USERPROFILE\.docker"
$daemonJsonPath = "$dockerConfigPath\daemon.json"

Write-Host "2. Docker registry mirror yapılandırılıyor..." -ForegroundColor Yellow

# .docker klasörünü oluştur
if (!(Test-Path $dockerConfigPath)) {
    New-Item -ItemType Directory -Path $dockerConfigPath -Force | Out-Null
}

# daemon.json yapılandırması
$daemonConfig = @{
    "registry-mirrors" = @(
        "https://mirror.gcr.io",
        "https://daocloud.io",
        "https://c.163.com",
        "https://registry.docker-cn.com"
    )
    "dns" = @("8.8.8.8", "8.8.4.4", "1.1.1.1")
    "max-concurrent-downloads" = 3
    "max-concurrent-uploads" = 5
}

$daemonConfig | ConvertTo-Json -Depth 3 | Set-Content -Path $daemonJsonPath -Encoding UTF8

Write-Host "3. daemon.json oluşturuldu:" -ForegroundColor Green
Write-Host $daemonJsonPath -ForegroundColor White
Get-Content $daemonJsonPath

Write-Host ""
Write-Host "4. Docker Desktop yeniden başlatılıyor..." -ForegroundColor Yellow
Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe"

Write-Host ""
Write-Host "5. Docker'ın başlamasını bekleyin (30 saniye)..." -ForegroundColor Yellow
$countdown = 30
while ($countdown -gt 0) {
    Write-Host "`rKalan süre: $countdown saniye" -NoNewline -ForegroundColor Cyan
    Start-Sleep -Seconds 1
    $countdown--
}
Write-Host ""
Write-Host ""

Write-Host "6. Docker durumu kontrol ediliyor..." -ForegroundColor Yellow
$maxAttempts = 10
$attempt = 1

while ($attempt -le $maxAttempts) {
    try {
        docker info > $null 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Docker başarıyla başladı!" -ForegroundColor Green
            break
        }
    } catch {}
    
    Write-Host "Docker henüz hazır değil. Deneme $attempt/$maxAttempts..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    $attempt++
}

Write-Host ""
Write-Host "TAMAMLANDI!" -ForegroundColor Green
Write-Host "============" -ForegroundColor Yellow
Write-Host ""
Write-Host "Şimdi Docker Desktop'ta:" -ForegroundColor Cyan
Write-Host "1. Settings > Kubernetes" -ForegroundColor White
Write-Host "2. 'Reset Kubernetes Cluster' butonuna basın" -ForegroundColor White
Write-Host "3. Kubernetes'i yeniden Enable edin" -ForegroundColor White
Write-Host ""
Write-Host "Registry mirror'lar eklendiği için download sorunu çözülmüş olmalı." -ForegroundColor Green