# Vitrin için Minikube Kurulum Script'i
# Docker Desktop Kubernetes sorunları için alternatif çözüm

Write-Host "Vitrin Minikube Kurulum Başlatılıyor..." -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Yellow
Write-Host ""

# Yönetici yetkisi kontrolü
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]$currentUser
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "Bu script yönetici yetkisiyle çalıştırılmalı!" -ForegroundColor Red
    Write-Host "PowerShell'i 'Run as Administrator' ile açın." -ForegroundColor Yellow
    exit 1
}

# Chocolatey kurulumu kontrol et
Write-Host "1. Chocolatey kurulumu kontrol ediliyor..." -ForegroundColor Cyan
if (!(Get-Command choco -ErrorAction SilentlyContinue)) {
    Write-Host "Chocolatey kuruluyor..." -ForegroundColor Yellow
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
    
    # PATH'i güncelle
    $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("PATH", "User")
}
Write-Host "Chocolatey hazır!" -ForegroundColor Green

# kubectl kurulumu
Write-Host ""
Write-Host "2. kubectl kuruluyor..." -ForegroundColor Cyan
try {
    choco install kubernetes-cli -y
    Write-Host "kubectl kuruldu!" -ForegroundColor Green
} catch {
    Write-Host "kubectl kurulumunda hata, devam ediliyor..." -ForegroundColor Yellow
}

# Minikube kurulumu
Write-Host ""
Write-Host "3. Minikube kuruluyor..." -ForegroundColor Cyan
try {
    choco install minikube -y
    Write-Host "Minikube kuruldu!" -ForegroundColor Green
} catch {
    Write-Host "Minikube kurulumunda hata, devam ediliyor..." -ForegroundColor Yellow
}

# Helm kurulumu
Write-Host ""
Write-Host "4. Helm kuruluyor..." -ForegroundColor Cyan
try {
    choco install kubernetes-helm -y
    Write-Host "Helm kuruldu!" -ForegroundColor Green
} catch {
    Write-Host "Helm kurulumunda hata, devam ediliyor..." -ForegroundColor Yellow
}

# PATH'i güncelle
Write-Host ""
Write-Host "5. PATH güncelleniyor..." -ForegroundColor Cyan
$env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("PATH", "User")

# Minikube cluster'ını başlat
Write-Host ""
Write-Host "6. Minikube cluster başlatılıyor..." -ForegroundColor Cyan
Write-Host "Bu işlem 3-5 dakika sürebilir..." -ForegroundColor Yellow

try {
    # Mevcut cluster'ı durdur (varsa)
    minikube stop 2>$null
    minikube delete 2>$null
    
    # Yeni cluster başlat
    minikube start --driver=docker --cpus=4 --memory=8192 --disk-size=20g --kubernetes-version=v1.28.0
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Minikube cluster başarıyla başlatıldı!" -ForegroundColor Green
        
        # Cluster durumunu kontrol et
        Write-Host ""
        Write-Host "7. Cluster durumu kontrol ediliyor..." -ForegroundColor Cyan
        kubectl cluster-info
        
        Write-Host ""
        Write-Host "Node durumu:" -ForegroundColor Cyan
        kubectl get nodes
        
        # Addons'ları etkinleştir
        Write-Host ""
        Write-Host "8. Kubernetes addons etkinleştiriliyor..." -ForegroundColor Cyan
        minikube addons enable ingress
        minikube addons enable dashboard
        minikube addons enable metrics-server
        
        Write-Host ""
        Write-Host "BAŞARILI! Minikube Kubernetes cluster'ı hazır!" -ForegroundColor Green
        Write-Host "=============================================" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Cluster Bilgileri:" -ForegroundColor Cyan
        Write-Host "- Kubernetes Version: v1.28.0" -ForegroundColor White
        Write-Host "- CPU: 4 cores" -ForegroundColor White
        Write-Host "- Memory: 8GB" -ForegroundColor White  
        Write-Host "- Disk: 20GB" -ForegroundColor White
        Write-Host "- Driver: Docker" -ForegroundColor White
        Write-Host ""
        Write-Host "Sonraki adım:" -ForegroundColor Cyan
        Write-Host "   cd ..\manifests" -ForegroundColor White
        Write-Host "   .\deploy-vitrin.ps1" -ForegroundColor White
        Write-Host ""
        Write-Host "Dashboard erişimi:" -ForegroundColor Cyan
        Write-Host "   minikube dashboard" -ForegroundColor White
        
    } else {
        Write-Host "Minikube başlatma hatası!" -ForegroundColor Red
        Write-Host "Docker Desktop'ın çalıştığından emin olun." -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "Minikube kurulumunda hata: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Manuel kurulum için:" -ForegroundColor Yellow
    Write-Host "1. Docker Desktop'ı tamamen kapatın" -ForegroundColor White
    Write-Host "2. Docker Desktop'ı yeniden açın" -ForegroundColor White
    Write-Host "3. Bu script'i tekrar çalıştırın" -ForegroundColor White
}

Write-Host ""
Write-Host "Tamamlandı!" -ForegroundColor Green