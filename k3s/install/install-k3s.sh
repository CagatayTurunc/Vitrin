#!/bin/bash

# Vitrin K3s Professional Setup Script
# Bu script K3s cluster'ını production-ready şekilde kurar

set -e

VITRIN_NAMESPACE="vitrin"
MONITORING_NAMESPACE="vitrin-monitoring"

echo "🚀 Vitrin K3s Professional Setup Başlatılıyor..."
echo "=================================================="

# K3s kurulumu (eğer kurulu değilse)
check_k3s() {
    if ! command -v k3s &> /dev/null; then
        echo "📦 K3s kuruluyor..."
        curl -sfL https://get.k3s.io | INSTALL_K3S_EXEC="--write-kubeconfig-mode 644 --disable traefik --disable servicelb" sh -
        
        # K3s'in başlamasını bekle
        echo "⏳ K3s servisinin başlamasını bekleniyor..."
        sleep 30
        
        # KUBECONFIG ayarla
        export KUBECONFIG=/etc/rancher/k3s/k3s.yaml
        echo 'export KUBECONFIG=/etc/rancher/k3s/k3s.yaml' >> ~/.bashrc
        
        echo "✅ K3s başarıyla kuruldu!"
    else
        echo "✅ K3s zaten kurulu!"
    fi
}

# Kubectl kurulumu
install_kubectl() {
    if ! command -v kubectl &> /dev/null; then
        echo "📦 kubectl kuruluyor..."
        curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
        chmod +x kubectl
        sudo mv kubectl /usr/local/bin/
        echo "✅ kubectl kuruldu!"
    else
        echo "✅ kubectl zaten kurulu!"
    fi
}

# Helm kurulumu
install_helm() {
    if ! command -v helm &> /dev/null; then
        echo "📦 Helm kuruluyor..."
        curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
        echo "✅ Helm kuruldu!"
    else
        echo "✅ Helm zaten kurulu!"
    fi
}

# NGINX Ingress Controller kurulumu
install_nginx_ingress() {
    echo "🌐 NGINX Ingress Controller kuruluyor..."
    helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
    helm repo update
    
    helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx \
        --namespace ingress-nginx \
        --create-namespace \
        --set controller.service.type=LoadBalancer \
        --set controller.metrics.enabled=true \
        --set controller.podAnnotations."prometheus\.io/scrape"="true" \
        --set controller.podAnnotations."prometheus\.io/port"="10254"
    
    echo "✅ NGINX Ingress Controller kuruldu!"
}

# Cert-Manager kurulumu (SSL/TLS için)
install_cert_manager() {
    echo "🔒 Cert-Manager kuruluyor..."
    helm repo add jetstack https://charts.jetstack.io
    helm repo update
    
    helm upgrade --install cert-manager jetstack/cert-manager \
        --namespace cert-manager \
        --create-namespace \
        --version v1.13.0 \
        --set installCRDs=true
    
    echo "✅ Cert-Manager kuruldu!"
}

# Longhorn Storage kurulumu (Persistent Volumes için)
install_longhorn() {
    echo "💾 Longhorn Storage kuruluyor..."
    helm repo add longhorn https://charts.longhorn.io
    helm repo update
    
    helm upgrade --install longhorn longhorn/longhorn \
        --namespace longhorn-system \
        --create-namespace \
        --set defaultSettings.defaultDataPath="/var/lib/longhorn/"
    
    echo "✅ Longhorn Storage kuruldu!"
}

# Ana kurulum fonksiyonu
main() {
    echo "🔧 Sistem gereksinimleri kontrol ediliyor..."
    
    # Root yetkisi kontrol et
    if [[ $EUID -eq 0 ]]; then
        echo "❌ Bu script root olarak çalıştırılmamalı!"
        exit 1
    fi
    
    # İşletim sistemi kontrol et
    if [[ "$OSTYPE" != "linux-gnu"* ]]; then
        echo "❌ Bu script yalnızca Linux üzerinde çalışır!"
        exit 1
    fi
    
    # Kurulum adımları
    check_k3s
    install_kubectl
    install_helm
    install_nginx_ingress
    install_cert_manager
    install_longhorn
    
    # Namespace'leri oluştur
    echo "📁 Namespace'ler oluşturuluyor..."
    kubectl create namespace $VITRIN_NAMESPACE --dry-run=client -o yaml | kubectl apply -f -
    kubectl create namespace $MONITORING_NAMESPACE --dry-run=client -o yaml | kubectl apply -f -
    
    echo ""
    echo "🎉 K3s Professional Setup Tamamlandı!"
    echo "=================================================="
    echo ""
    echo "📋 Kurulum Özeti:"
    echo "  ✅ K3s Cluster"
    echo "  ✅ kubectl CLI"
    echo "  ✅ Helm Package Manager"
    echo "  ✅ NGINX Ingress Controller"
    echo "  ✅ Cert-Manager (SSL/TLS)"
    echo "  ✅ Longhorn Storage"
    echo "  ✅ Vitrin Namespaces"
    echo ""
    echo "🔍 Cluster Durumu:"
    kubectl get nodes
    echo ""
    echo "📝 Sonraki Adım:"
    echo "   cd ../manifests && ./deploy-vitrin.sh"
    echo ""
}

main "$@"