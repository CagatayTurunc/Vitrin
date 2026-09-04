# ============================================================
# Vitrin Subscription Quick Test
# Kullanım: .\scripts\test-subscription-simple.ps1
# ============================================================

$BaseUrl = "http://localhost:5104"  # Auth service direkt portu
$TestEmail = "test-$(Get-Random)@vitrin.test"
$TestPassword = "Test1234!"

Write-Host "🚀 Vitrin Subscription Test Başlatılıyor..." -ForegroundColor Cyan
Write-Host "   Email: $TestEmail" -ForegroundColor Gray

# Test 1: Health Check
Write-Host "`n1️⃣  Health Check..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/health" -Method GET
    Write-Host "   ✅ Servis çalışıyor!" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Servis ulaşılamıyor: $_" -ForegroundColor Red
    exit 1
}

# Test 2: Kullanıcı Kaydı
Write-Host "`n2️⃣  Kullanıcı Kaydı..." -ForegroundColor Yellow
try {
    $registerBody = @{
        email = $TestEmail
        password = $TestPassword
        username = "testuser$(Get-Random -Maximum 9999)"
        fullName = "Test User"
    } | ConvertTo-Json

    $register = Invoke-RestMethod -Uri "$BaseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
    $token = $register.token
    Write-Host "   ✅ Kayıt başarılı! Token alındı." -ForegroundColor Green
} catch {
    Write-Host "   ❌ Kayıt hatası: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 3: Abonelik Durumu
Write-Host "`n3️⃣  Abonelik Durumu (Free)..." -ForegroundColor Yellow
try {
    $headers = @{ Authorization = "Bearer $token" }
    $sub = Invoke-RestMethod -Uri "$BaseUrl/api/subscription/me" -Method GET -Headers $headers
    Write-Host "   ✅ Tier: $($sub.tier), Status: $($sub.status)" -ForegroundColor Green
    Write-Host "   📊 Quota: $($sub.usage.productsCreated)/$($sub.usage.maxProducts) ürün" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Abonelik bilgisi alınamadı: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Kupon Doğrulama (geçersiz kod)
Write-Host "`n4️⃣  Kupon Doğrulama (geçersiz)..." -ForegroundColor Yellow
try {
    $headers = @{ Authorization = "Bearer $token" }
    $couponBody = @{ code = "INVALID123"; tier = 1 } | ConvertTo-Json
    $coupon = Invoke-RestMethod -Uri "$BaseUrl/api/discount/validate" -Method POST -Body $couponBody -ContentType "application/json" -Headers $headers
    
    if ($coupon.valid -eq $false) {
        Write-Host "   ✅ Geçersiz kupon doğru şekilde reddedildi" -ForegroundColor Green
        Write-Host "      Hata: $($coupon.errorMessage)" -ForegroundColor Gray
    } else {
        Write-Host "   ❌ Geçersiz kupon kabul edildi!" -ForegroundColor Red
    }
} catch {
    Write-Host "   ❌ Kupon testi hatası: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Checkout Başlatma
Write-Host "`n5️⃣  Checkout Başlatma (ProMaker)..." -ForegroundColor Yellow
try {
    $headers = @{ Authorization = "Bearer $token" }
    $checkoutBody = @{ tier = 1; couponCode = $null } | ConvertTo-Json
    $checkout = Invoke-RestMethod -Uri "$BaseUrl/api/subscription/checkout" -Method POST -Body $checkoutBody -ContentType "application/json" -Headers $headers
    
    Write-Host "   ✅ İyzico checkout session oluşturuldu!" -ForegroundColor Green
    Write-Host "      URL: $($checkout.checkoutUrl.Substring(0, [Math]::Min(60, $checkout.checkoutUrl.Length)))..." -ForegroundColor Gray
    Write-Host "`n   🔗 TEST KARTLARI (İyzico Sandbox):" -ForegroundColor Cyan
    Write-Host "      Kart No : 5528790000000008" -ForegroundColor White
    Write-Host "      SKT     : 12/30" -ForegroundColor White
    Write-Host "      CVC     : 123" -ForegroundColor White
    Write-Host "      3D Şifre: a" -ForegroundColor White
} catch {
    Write-Host "   ❌ Checkout hatası: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 6: Ödeme Geçmişi
Write-Host "`n6️⃣  Ödeme Geçmişi..." -ForegroundColor Yellow
try {
    $headers = @{ Authorization = "Bearer $token" }
    $invoices = Invoke-RestMethod -Uri "$BaseUrl/api/subscription/invoices" -Method GET -Headers $headers
    Write-Host "   ✅ Fatura sayısı: $($invoices.Count)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Fatura listesi alınamadı: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 7: Admin Stats (yetkisiz olmalı)
Write-Host "`n7️⃣  Admin Stats (403 bekleniyor)..." -ForegroundColor Yellow
try {
    $headers = @{ Authorization = "Bearer $token" }
    $stats = Invoke-RestMethod -Uri "$BaseUrl/api/subscription/admin/stats" -Method GET -Headers $headers
    Write-Host "   ❌ Admin endpoint kullanıcıya açık!" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 403) {
        Write-Host "   ✅ Admin endpoint doğru şekilde korunuyor (403)" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Beklenmeyen hata: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host "`n✨ Test Tamamlandı!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
