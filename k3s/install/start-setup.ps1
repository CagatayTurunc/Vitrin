# Vitrin K3s Setup Başlatıcı
# Bu script Docker Desktop Kubernetes kurulumunu kontrol eder ve rehberlik eder

Write-Host "Vitrin K3s Kurulum Başlatıcısı" -ForegroundColor Green
Write-Host "===============================" -ForegroundColor Yellow
Write-Host ""

# Docker Desktop durumunu kontrol et
Write-Host "Docker Desktop durumu kontrol ediliyor..." -ForegroundColor Cyan

try {
    $dockerVersion = docker --version
    Write-Host "Docker kurulu: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "Docker bulunamadı! Lütfen Docker Desktop'ı kurun." -ForegroundColor Red
    Write-Host "   https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
    exit 1
}

# Kubernetes cluster bağlantısını test et
Write-Host ""
Write-Host "Kubernetes cluster bağlantısı test ediliyor..." -ForegroundColor Cyan

try {
    kubectl cluster-info --request-timeout=5s | Out-Null
    Write-Host "Kubernetes cluster aktif ve bağlantı başarılı!" -ForegroundColor Green
    
    # Cluster bilgilerini göster
    Write-Host ""
    Write-Host "Cluster Bilgileri:" -ForegroundColor Cyan
    kubectl cluster-info
    
    Write-Host ""
    Write-Host "Kubernetes hazır! Vitrin deployment'ına geçebiliriz." -ForegroundColor Green
    Write-Host ""
    Write-Host "Sonraki adım:" -ForegroundColor Cyan
    Write-Host "   cd ..\manifests" -ForegroundColor White
    Write-Host "   .\deploy-vitrin.ps1" -ForegroundColor White
    
} catch {
    Write-Host "Kubernetes cluster'ına bağlanılamıyor." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Docker Desktop'ta Kubernetes'i Aktifleştirin:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. Docker Desktop'ı açın (system tray'den)" -ForegroundColor White
    Write-Host "2. Settings > Kubernetes" -ForegroundColor White  
    Write-Host "3. Enable Kubernetes işaretleyin" -ForegroundColor White
    Write-Host "4. Apply & Restart butonuna basın" -ForegroundColor White
    Write-Host "5. Kubernetes'in başlamasını bekleyin (yeşil dot)" -ForegroundColor White
    Write-Host ""
    Write-Host "Kurulum 2-5 dakika sürebilir..." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Kurulum tamamlandıktan sonra bu script'i tekrar çalıştırın:" -ForegroundColor Green
    Write-Host "   .\start-setup.ps1" -ForegroundColor White
    Write-Host ""
    
    # Docker Desktop'ı otomatik açmaya çalış
    Write-Host "Docker Desktop'ı otomatik açmaya çalışıyor..." -ForegroundColor Cyan
    try {
        Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe" -ErrorAction SilentlyContinue
        Write-Host "Docker Desktop açıldı!" -ForegroundColor Green
    } catch {
        Write-Host "Docker Desktop otomatik açılamadı. Manuel olarak açın." -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Yardım için: https://docs.docker.com/desktop/kubernetes/" -ForegroundColor Blue