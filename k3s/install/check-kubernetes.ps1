# Kubernetes durumunu kontrol eden script
Write-Host "Kubernetes Cluster Durumu Kontrol Ediliyor..." -ForegroundColor Cyan
Write-Host ""

$maxAttempts = 10
$attempt = 1

while ($attempt -le $maxAttempts) {
    Write-Host "Deneme $attempt/$maxAttempts..." -ForegroundColor Yellow
    
    try {
        # Cluster bilgilerini al
        $clusterInfo = kubectl cluster-info 2>$null
        
        if ($clusterInfo -match "Kubernetes control plane") {
            Write-Host "BASARILI! Kubernetes cluster aktif!" -ForegroundColor Green
            Write-Host ""
            Write-Host "Cluster Bilgileri:" -ForegroundColor Cyan
            kubectl cluster-info
            Write-Host ""
            
            # Node'ları kontrol et
            Write-Host "Node Durumu:" -ForegroundColor Cyan
            kubectl get nodes
            Write-Host ""
            
            Write-Host "Kubernetes hazir! Vitrin deployment'ina geçebiliriz." -ForegroundColor Green
            Write-Host ""
            Write-Host "Sonraki adim:" -ForegroundColor Cyan
            Write-Host "   cd ..\manifests" -ForegroundColor White
            Write-Host "   .\deploy-vitrin.ps1" -ForegroundColor White
            
            exit 0
        }
    } catch {
        # Bağlantı hatası
    }
    
    Write-Host "Kubernetes henüz hazır değil. 10 saniye bekleniyor..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
    $attempt++
}

Write-Host "Kubernetes cluster'a bağlanılamadı." -ForegroundColor Red
Write-Host "Docker Desktop'ta Kubernetes'in etkin olduğundan emin olun." -ForegroundColor Yellow