#!/bin/bash
# Email doğrulama hotfix script
set -e

echo "🔧 E-posta doğrulama hotfix başlıyor..."

# Sadece kritik servisleri restart et - auth, gateway, web
echo "📡 Auth servisini yeniden başlatıyor..."
docker restart vitrin-auth

echo "🌐 Gateway servisini yeniden başlatıyor..."
docker restart vitrin-gateway

echo "💻 Web servisini yeniden başlatıyor..."
docker restart vitrin-web

# Servislerin sağlık durumunu kontrol et
echo "🩺 Servis sağlık kontrolleri..."
sleep 10

# Auth servisi kontrolü
if docker exec vitrin-auth curl -f http://localhost:8080/health >/dev/null 2>&1; then
    echo "✅ Auth servisi sağlıklı"
else
    echo "❌ Auth servisi sağlıksız"
fi

# Gateway kontrolü
if curl -f http://localhost:5000/health >/dev/null 2>&1; then
    echo "✅ Gateway sağlıklı"
else
    echo "❌ Gateway sağlıksız"
fi

# Web servisi kontrolü (Docker network içinden)
if docker exec vitrin-web curl -f http://localhost:3000/api/health >/dev/null 2>&1; then
    echo "✅ Web servisi sağlıklı"
else
    echo "⚠️  Web servisi kontrol edilemiyor (normal olabilir)"
fi

echo "🎉 Hotfix tamamlandı! E-posta doğrulama test edilebilir."
echo "🧪 Test: https://vitrin.it.com/register"