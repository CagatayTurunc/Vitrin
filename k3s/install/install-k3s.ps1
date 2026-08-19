# Vitrin K3s Professional Setup Script (Windows)
# Bu script K3s cluster'ını Windows üzerinde production-ready şekilde kurar

param(
    [switch]$SkipK3s,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

$VITRIN_NAMESPACE = "vitrin"
$MONITORING_NAMESPACE = "vitrin-monitoring"

Write-Host "🚀 Vitrin K3s Professional Setup Başlatılıyor..." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Yellow

# K3s kurulumu kontrolü
function Test-K3sInstalled {
    try {
        $null = Get-Command k3s -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

# Kubectl kurulumu
function Install-Kubectl {
    if (!(Get-Command kubectl -ErrorAction SilentlyContinue)) {
        Write-Host "📦 kubectl kuruluyor..." -ForegroundColor Yellow
        
        # Chocolatey ile kur (eğer varsa)
        if (Get-Command choco -ErrorAction SilentlyContinue) {
            choco install kubernetes-cli -y
        }
        else {
            # Manuel kurulum
            $kubectlUrl = "https://dl.k8s.io/release/v1.28.0/bin/windows/amd64/kubectl.exe"
            $kubectlPath = "$env:TEMP\kubectl.exe"
            
            Invoke-WebRequest -Uri $kubectlUrl -OutFile $kubectlPath
            
            # Program Files'a kopyala
            $targetPath = "$env:ProgramFiles\kubectl"
            if (!(Test-Path $targetPath)) {
                New-Item -ItemType Directory -Path $targetPath -Force
            }
            Copy-Item $kubectlPath "$targetPath\kubectl.exe" -Force
            
            # PATH'e ekle
            $currentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
            if ($currentPath -notlike "*$targetPath*") {
                [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$targetPath", "Machine")
            }
        }
        Write-Host "✅ kubectl kuruldu!" -ForegroundColor Green
    }
    else {
        Write-Host "✅ kubectl zaten kurulu!" -ForegroundColor Green
    }
}

# Helm kurulumu
function Install-Helm {
    if (!(Get-Command helm -ErrorAction SilentlyContinue)) {
        Write-Host "📦 Helm kuruluyor..." -ForegroundColor Yellow
        
        if (Get-Command choco -ErrorAction SilentlyContinue) {
            choco install kubernetes-helm -y
        }
        else {
            # Manuel kurulum
            $helmUrl = "https://get.helm.sh/helm-v3.12.0-windows-amd64.zip"
            $helmZip = "$env:TEMP\helm.zip"
            $extractPath = "$env:TEMP\helm"
            
            Invoke-WebRequest -Uri $helmUrl -OutFile $helmZip
            Expand-Archive $helmZip $extractPath -Force
            
            $targetPath = "$env:ProgramFiles\helm"
            if (!(Test-Path $targetPath)) {
                New-Item -ItemType Directory -Path $targetPath -Force
            }
            
            Copy-Item "$extractPath\windows-amd64\helm.exe" "$targetPath\helm.exe" -Force
            
            # PATH'e ekle
            $currentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
            if ($currentPath -notlike "*$targetPath*") {
                [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$targetPath", "Machine")
            }
        }
        Write-Host "✅ Helm kuruldu!" -ForegroundColor Green
    }
    else {
        Write-Host "✅ Helm zaten kurulu!" -ForegroundColor Green
    }
}

# Docker Desktop K3s kurulumu
function Install-K3sDockerDesktop {
    Write-Host "🐳 Docker Desktop ile K3s kurulumu..." -ForegroundColor Yellow
    Write-Host "📋 Manuel Adımlar:" -ForegroundColor Cyan
    Write-Host "   1. Docker Desktop'ı açın" -ForegroundColor White
    Write-Host "   2. Settings > Kubernetes > Enable Kubernetes" -ForegroundColor White
    Write-Host "   3. Apply & Restart" -ForegroundColor White
    Write-Host ""
    Write-Host "⚠️  Alternatif olarak WSL2 üzerinde K3s kurabilirsiniz:" -ForegroundColor Yellow
    Write-Host "   wsl --install -d Ubuntu-22.04" -ForegroundColor White
    Write-Host "   wsl -d Ubuntu-22.04" -ForegroundColor White
    Write-Host "   curl -sfL https://get.k3s.io | sh -" -ForegroundColor White
}

# NGINX Ingress Controller kurulumu
function Install-NginxIngress {
    Write-Host "🌐 NGINX Ingress Controller kuruluyor..." -ForegroundColor Yellow
    
    helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
    helm repo update
    
    helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx `
        --namespace ingress-nginx `
        --create-namespace `
        --set controller.service.type=LoadBalancer `
        --set controller.metrics.enabled=true `
        --set-string controller.podAnnotations."prometheus\.io/scrape"="true" `
        --set-string controller.podAnnotations."prometheus\.io/port"="10254"
    
    Write-Host "✅ NGINX Ingress Controller kuruldu!" -ForegroundColor Green
}

# Cert-Manager kurulumu
function Install-CertManager {
    Write-Host "🔒 Cert-Manager kuruluyor..." -ForegroundColor Yellow
    
    helm repo add jetstack https://charts.jetstack.io
    helm repo update
    
    helm upgrade --install cert-manager jetstack/cert-manager `
        --namespace cert-manager `
        --create-namespace `
        --version v1.13.0 `
        --set installCRDs=true
    
    Write-Host "✅ Cert-Manager kuruldu!" -ForegroundColor Green
}

# Ana kurulum fonksiyonu
function Main {
    Write-Host "🔧 Sistem gereksinimleri kontrol ediliyor..." -ForegroundColor Yellow
    
    # Yönetici yetkisi kontrolü
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]$currentUser
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "❌ Bu script yönetici yetkisiyle çalıştırılmalı!" -ForegroundColor Red
        Write-Host "PowerShell'i 'Run as Administrator' ile açın." -ForegroundColor Yellow
        exit 1
    }
    
    # Kurulum adımları
    if (-not $SkipK3s) {
        if (-not (Test-K3sInstalled)) {
            Install-K3sDockerDesktop
            Write-Host "⏸️  K3s kurulumunu tamamladıktan sonra script'i tekrar çalıştırın: .\install-k3s.ps1 -SkipK3s" -ForegroundColor Yellow
            return
        }
        else {
            Write-Host "✅ K3s zaten kurulu!" -ForegroundColor Green
        }
    }
    
    Install-Kubectl
    Install-Helm
    
    # Kubernetes bağlantısı kontrol et
    try {
        kubectl cluster-info | Out-Null
        Write-Host "✅ Kubernetes cluster'ına bağlantı başarılı!" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Kubernetes cluster'ına bağlanılamıyor!" -ForegroundColor Red
        Write-Host "Docker Desktop'ta Kubernetes'in etkin olduğundan emin olun." -ForegroundColor Yellow
        return
    }
    
    Install-NginxIngress
    Install-CertManager
    
    # Namespace'leri oluştur
    Write-Host "📁 Namespace'ler oluşturuluyor..." -ForegroundColor Yellow
    kubectl create namespace $VITRIN_NAMESPACE --dry-run=client -o yaml | kubectl apply -f -
    kubectl create namespace $MONITORING_NAMESPACE --dry-run=client -o yaml | kubectl apply -f -
    
    Write-Host ""
    Write-Host "🎉 K3s Professional Setup Tamamlandı!" -ForegroundColor Green
    Write-Host "==================================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "📋 Kurulum Özeti:" -ForegroundColor Cyan
    Write-Host "  ✅ Kubernetes Cluster (Docker Desktop)" -ForegroundColor White
    Write-Host "  ✅ kubectl CLI" -ForegroundColor White
    Write-Host "  ✅ Helm Package Manager" -ForegroundColor White
    Write-Host "  ✅ NGINX Ingress Controller" -ForegroundColor White
    Write-Host "  ✅ Cert-Manager (SSL/TLS)" -ForegroundColor White
    Write-Host "  ✅ Vitrin Namespaces" -ForegroundColor White
    Write-Host ""
    Write-Host "🔍 Cluster Durumu:" -ForegroundColor Cyan
    kubectl get nodes
    Write-Host ""
    Write-Host "📝 Sonraki Adım:" -ForegroundColor Cyan
    Write-Host "   cd ..\manifests" -ForegroundColor White
    Write-Host "   .\deploy-vitrin.ps1" -ForegroundColor White
    Write-Host ""
}

Main