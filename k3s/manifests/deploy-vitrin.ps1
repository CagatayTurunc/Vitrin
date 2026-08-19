# Vitrin K3s Professional Deployment Script
# Bu script Vitrin mikroservis platformunu K3s üzerine deploy eder

param(
    [switch]$SkipInfrastructure,
    [switch]$SkipApplications,
    [switch]$SkipMonitoring,
    [switch]$DryRun,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Renkli output için
$Colors = @{
    Green   = "Green"
    Yellow  = "Yellow" 
    Red     = "Red"
    Cyan    = "Cyan"
    White   = "White"
    Blue    = "Blue"
}

function Write-StatusMessage {
    param(
        [string]$Message,
        [string]$Color = "White",
        [string]$Icon = "ℹ️"
    )
    Write-Host "$Icon $Message" -ForegroundColor $Colors[$Color]
}

function Test-KubernetesConnection {
    try {
        kubectl cluster-info --request-timeout=10s | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Test-NamespaceExists {
    param([string]$Namespace)
    try {
        kubectl get namespace $Namespace -o name 2>$null | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Wait-ForPodsReady {
    param(
        [string]$Namespace,
        [string]$LabelSelector,
        [int]$TimeoutSeconds = 300
    )
    
    Write-StatusMessage "Pod'ların hazır olması bekleniyor: $LabelSelector" "Yellow" "⏳"
    
    if ($DryRun) {
        Write-StatusMessage "DRY RUN: kubectl wait komutu simüle edildi" "Cyan" "🔍"
        return
    }
    
    $command = "kubectl wait --for=condition=ready pod -l $LabelSelector -n $Namespace --timeout=${TimeoutSeconds}s"
    
    try {
        Invoke-Expression $command
        Write-StatusMessage "Pod'lar başarıyla hazır!" "Green" "✅"
    }
    catch {
        Write-StatusMessage "Pod'ların hazır olmasında sorun: $_" "Red" "❌"
        throw
    }
}

function Deploy-Manifest {
    param(
        [string]$FilePath,
        [string]$Description
    )
    
    if (!(Test-Path $FilePath)) {
        Write-StatusMessage "$Description dosyası bulunamadı: $FilePath" "Red" "❌"
        throw "Manifest dosyası bulunamadı"
    }
    
    Write-StatusMessage "$Description kuruluyor..." "Yellow" "🚀"
    
    if ($DryRun) {
        Write-StatusMessage "DRY RUN: kubectl apply -f $FilePath" "Cyan" "🔍"
        return
    }
    
    try {
        kubectl apply -f $FilePath
        Write-StatusMessage "$Description başarıyla kuruldu!" "Green" "✅"
    }
    catch {
        Write-StatusMessage "$Description kurulumunda hata: $_" "Red" "❌"
        throw
    }
}

function Main {
    Write-Host ""
    Write-Host "🚀 Vitrin K3s Professional Deployment" -ForegroundColor $Colors["Green"]
    Write-Host "====================================" -ForegroundColor $Colors["Yellow"]
    Write-Host ""
    
    if ($DryRun) {
        Write-StatusMessage "DRY RUN modunda çalışıyor - hiçbir değişiklik yapılmayacak" "Cyan" "🔍"
        Write-Host ""
    }
    
    # Kubernetes bağlantısını kontrol et
    Write-StatusMessage "Kubernetes cluster bağlantısı kontrol ediliyor..." "Blue" "🔍"
    if (!(Test-KubernetesConnection)) {
        Write-StatusMessage "Kubernetes cluster'ına bağlanılamıyor!" "Red" "❌"
        Write-StatusMessage "Lütfen K3s'in çalıştığından emin olun:" "Yellow" "💡"
        Write-StatusMessage "  sudo systemctl status k3s" "White" "   "
        Write-StatusMessage "  kubectl cluster-info" "White" "   "
        exit 1
    }
    Write-StatusMessage "Kubernetes cluster bağlantısı başarılı!" "Green" "✅"
    
    # Deployment aşamaları
    $deploymentSteps = @()
    
    if (!$SkipInfrastructure) {
        $deploymentSteps += @{
            Phase = "Infrastructure"
            Steps = @(
                @{ File = "00-namespace.yaml"; Description = "Namespace'ler" },
                @{ File = "01-configmap.yaml"; Description = "Konfigürasyon" },
                @{ File = "02-secrets.yaml"; Description = "Secrets" },
                @{ File = "03-storage.yaml"; Description = "Storage Classes ve PVC'ler" }
            )
        }
        
        $deploymentSteps += @{
            Phase = "Data Layer"
            Steps = @(
                @{ File = "04-postgres.yaml"; Description = "PostgreSQL Database" },
                @{ File = "05-redis.yaml"; Description = "Redis Cache" },
                @{ File = "06-kafka.yaml"; Description = "Kafka Message Queue" }
            )
        }
    }
    
    if (!$SkipApplications) {
        $deploymentSteps += @{
            Phase = "Application Services"
            Steps = @(
                @{ File = "07-auth-service.yaml"; Description = "Auth Service" },
                @{ File = "08-product-service.yaml"; Description = "Product Service" },
                @{ File = "09-voting-service.yaml"; Description = "Voting Service" },
                @{ File = "10-comment-service.yaml"; Description = "Comment Service" },
                @{ File = "11-notification-service.yaml"; Description = "Notification Service" },
                @{ File = "12-analytics-service.yaml"; Description = "Analytics Service" },
                @{ File = "13-ai-service.yaml"; Description = "AI Service" },
                @{ File = "14-api-gateway.yaml"; Description = "API Gateway" },
                @{ File = "15-web-ui.yaml"; Description = "Web UI" }
            )
        }
    }
    
    if (!$SkipMonitoring) {
        $deploymentSteps += @{
            Phase = "Monitoring Stack"
            Steps = @(
                @{ File = "16-prometheus.yaml"; Description = "Prometheus" },
                @{ File = "17-grafana.yaml"; Description = "Grafana" },
                @{ File = "18-jaeger.yaml"; Description = "Jaeger Tracing" }
            )
        }
    }
    
    # Deployment'ı çalıştır
    foreach ($phase in $deploymentSteps) {
        Write-Host ""
        Write-StatusMessage "$($phase.Phase) Deployment Aşaması" "Blue" "🏗️"
        Write-Host "$('-' * 50)" -ForegroundColor $Colors["Blue"]
        
        foreach ($step in $phase.Steps) {
            Deploy-Manifest -FilePath $step.File -Description $step.Description
            
            if (!$DryRun) {
                Start-Sleep -Seconds 2
            }
        }
        
        # Infrastructure aşamasından sonra pod'ların hazır olmasını bekle
        if ($phase.Phase -eq "Data Layer" -and !$DryRun -and !$SkipInfrastructure) {
            Write-Host ""
            Write-StatusMessage "Infrastructure servislerinin hazır olması bekleniyor..." "Yellow" "⏳"
            
            try {
                Wait-ForPodsReady -Namespace "vitrin" -LabelSelector "app.kubernetes.io/component=database" -TimeoutSeconds 180
                Wait-ForPodsReady -Namespace "vitrin" -LabelSelector "app.kubernetes.io/component=cache" -TimeoutSeconds 120
                Wait-ForPodsReady -Namespace "vitrin" -LabelSelector "app.kubernetes.io/component=coordination" -TimeoutSeconds 120
                Wait-ForPodsReady -Namespace "vitrin" -LabelSelector "app.kubernetes.io/component=message-queue" -TimeoutSeconds 120
            }
            catch {
                Write-StatusMessage "Infrastructure servislerinin başlatılmasında sorun var. Devam ediliyor..." "Yellow" "⚠️"
            }
        }
    }
    
    Write-Host ""
    Write-StatusMessage "Deployment Status Kontrolü" "Blue" "🔍"
    Write-Host "$(('-' * 50))" -ForegroundColor $Colors["Blue"]
    
    if (!$DryRun) {
        # Namespace'lerdeki pod'ları listele
        Write-StatusMessage "Vitrin Namespace Pod Durumu:" "Cyan" "📋"
        kubectl get pods -n vitrin -o wide
        
        Write-Host ""
        Write-StatusMessage "Vitrin Services:" "Cyan" "🌐"
        kubectl get services -n vitrin
        
        if (!$SkipMonitoring -and (Test-NamespaceExists "vitrin-monitoring")) {
            Write-Host ""
            Write-StatusMessage "Monitoring Namespace Pod Durumu:" "Cyan" "📋"
            kubectl get pods -n vitrin-monitoring -o wide
        }
        
        Write-Host ""
        Write-StatusMessage "Ingress Durumu:" "Cyan" "🌍"
        kubectl get ingress -n vitrin 2>$null
    }
    
    Write-Host ""
    Write-StatusMessage "✅ Vitrin K3s Deployment Tamamlandı!" "Green" "🎉"
    Write-Host "$(('=' * 50))" -ForegroundColor $Colors["Green"]
    Write-Host ""
    
    # Sonraki adımlar
    Write-StatusMessage "📝 Sonraki Adımlar:" "Cyan" "📋"
    Write-StatusMessage "1. Container image'larınızı build edin ve registry'ye push edin" "White" "   "
    Write-StatusMessage "2. secrets.yaml dosyasındaki credentials'ları güncelleyin" "White" "   "
    Write-StatusMessage "3. Ingress domain adreslerini gerçek domain'lerinizle değiştirin" "White" "   "
    Write-StatusMessage "4. Monitoring dashboard'larını import edin" "White" "   "
    Write-Host ""
    
    # Erişim bilgileri
    Write-StatusMessage "🌐 Erişim Bilgileri:" "Cyan" "🔗"
    Write-StatusMessage "Grafana: kubectl port-forward svc/grafana-service 3000:3000 -n vitrin-monitoring" "White" "   "
    Write-StatusMessage "Prometheus: kubectl port-forward svc/prometheus-service 9090:9090 -n vitrin-monitoring" "White" "   "
    Write-StatusMessage "Jaeger: kubectl port-forward svc/jaeger-service 16686:16686 -n vitrin-monitoring" "White" "   "
    Write-Host ""
    
    # Troubleshooting
    Write-StatusMessage "🔧 Troubleshooting:" "Cyan" "🔍"
    Write-StatusMessage "Tüm pod logları: kubectl logs -f -l app.kubernetes.io/part-of=vitrin-platform -n vitrin" "White" "   "
    Write-StatusMessage "Specific service log: kubectl logs -f deployment/DEPLOYMENT_NAME -n vitrin" "White" "   "
    Write-StatusMessage "Pod durumu detay: kubectl describe pod POD_NAME -n vitrin" "White" "   "
}

# Script başlat
try {
    Main
}
catch {
    Write-StatusMessage "Deployment sırasında hata oluştu: $_" "Red" "❌"
    exit 1
}