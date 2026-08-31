# ============================================================
# Vitrin Subscription & Payment Flow Test Script
# Kullanım: .\scripts\test-subscription.ps1
# ============================================================

param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$TestEmail = "test-sub-$(Get-Random)@vitrin-test.com",
    [string]$TestPassword = "Test1234!"
)

$ErrorActionPreference = "Continue"

function Write-Step($msg) { Write-Host "`n▶ $msg" -ForegroundColor Cyan }
function Write-Pass($msg) { Write-Host "  ✅ $msg" -ForegroundColor Green }
function Write-Fail($msg) { Write-Host "  ❌ $msg" -ForegroundColor Red }
function Write-Info($msg) { Write-Host "  ℹ  $msg" -ForegroundColor Gray }

$results = @()

function Test-Endpoint($name, $method, $url, $body, $token, $expectedStatus) {
    try {
        $headers = @{ "Content-Type" = "application/json" }
        if ($token) { $headers["Authorization"] = "Bearer $token" }

        $params = @{
            Method = $method
            Uri = $url
            Headers = $headers
            ErrorAction = "Stop"
        }
        if ($body) { $params["Body"] = ($body | ConvertTo-Json -Depth 5) }

        $response = Invoke-WebRequest @params
        $status = $response.StatusCode
        $content = $response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue

        if ($status -eq $expectedStatus -or ($expectedStatus -eq 200 -and $status -lt 300)) {
            Write-Pass "$name → HTTP $status"
            $results += [PSCustomObject]@{ Test = $name; Status = "PASS"; Code = $status }
            return $content
        } else {
            Write-Fail "$name → Beklenen: $expectedStatus, Alınan: $status"
            $results += [PSCustomObject]@{ Test = $name; Status = "FAIL"; Code = $status }
            return $null
        }
    } catch {
        $code = $_.Exception.Response?.StatusCode.value__ ?? "ERR"
        Write-Fail "$name → $code — $($_.Exception.Message)"
        $results += [PSCustomObject]@{ Test = $name; Status = "FAIL"; Code = $code }
        return $null
    }
}

Write-Host "============================================================" -ForegroundColor Yellow
Write-Host " Vitrin Subscription Test — $BaseUrl" -ForegroundColor Yellow
Write-Host " Test kullanıcı: $TestEmail" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Yellow

# ─── 1. Health Check ───────────────────────────────────────────
Write-Step "1. Health Check"
Test-Endpoint "GET /health" "GET" "$BaseUrl/health" $null $null 200

# ─── 2. Kayıt ──────────────────────────────────────────────────
Write-Step "2. Kullanıcı Kaydı"
$registerBody = @{
    email = $TestEmail
    password = $TestPassword
    username = "testuser$(Get-Random -Maximum 9999)"
    fullName = "Test User"
}
$registerResult = Test-Endpoint "POST /api/auth/register" "POST" "$BaseUrl/api/auth/register" $registerBody $null 200
if ($registerResult) { Write-Info "Token alındı: $($registerResult.token?.Substring(0,20))..." }

# ─── 3. Login ──────────────────────────────────────────────────
Write-Step "3. Login"
$loginBody = @{ email = $TestEmail; password = $TestPassword }
$loginResult = Test-Endpoint "POST /api/auth/login" "POST" "$BaseUrl/api/auth/login" $loginBody $null 200
$token = $loginResult?.token
if ($token) { Write-Info "JWT token alındı." } else { Write-Fail "Token alınamadı, sonraki testler atlanacak." }

# ─── 4. Abonelik Durumu (Free) ─────────────────────────────────
Write-Step "4. Mevcut Abonelik Durumu (Free olmalı)"
$subResult = Test-Endpoint "GET /api/subscription/me" "GET" "$BaseUrl/api/subscription/me" $null $token 200
if ($subResult) { Write-Info "Tier: $($subResult.tier), Status: $($subResult.status)" }

# ─── 5. Kupon Doğrulama (geçersiz) ─────────────────────────────
Write-Step "5. Geçersiz Kupon Doğrulama"
$couponBody = @{ code = "YOKKOD"; tier = 1 }
$couponResult = Test-Endpoint "POST /api/discount/validate" "POST" "$BaseUrl/api/discount/validate" $couponBody $token 200
if ($couponResult) {
    if ($couponResult.valid -eq $false) {
        Write-Pass "Geçersiz kupon reddedildi: $($couponResult.errorMessage)"
    } else {
        Write-Fail "Geçersiz kupon kabul edildi!"
    }
}

# ─── 6. Admin: Test Kuponu Oluştur ────────────────────────────
Write-Step "6. Admin: Test Kuponu Oluştur (LAUNCH50)"
$createCouponBody = @{
    code = "LAUNCH50TEST$(Get-Random -Maximum 999)"
    description = "Test kuponu - %50 indirim"
    type = 0
    value = 50
    applicableTiers = @(1, 2)
    maxUses = 100
    maxUsesPerUser = 1
    durationMonths = $null
    startsAt = $null
    expiresAt = (Get-Date).AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ")
}
$couponCode = $createCouponBody.code
# Not: Admin yetkisi gerekiyor — token admin değilse 403 alırız
$createCouponResult = Test-Endpoint "POST /api/discount/admin/create" "POST" "$BaseUrl/api/discount/admin/create" $createCouponBody $token 201

# ─── 7. Checkout Başlatma (İyzico Sandbox) ────────────────────
Write-Step "7. Checkout Başlatma (ProMaker - İyzico Sandbox)"
$checkoutBody = @{ tier = 1; couponCode = $null }
$checkoutResult = Test-Endpoint "POST /api/subscription/checkout" "POST" "$BaseUrl/api/subscription/checkout" $checkoutBody $token 200
if ($checkoutResult) {
    Write-Info "Checkout URL: $($checkoutResult.checkoutUrl?.Substring(0, [Math]::Min(60, $checkoutResult.checkoutUrl.Length)))..."
    Write-Info "Token: $($checkoutResult.token)"
    Write-Pass "İyzico checkout session oluşturuldu!"
}

# ─── 8. Ödeme Geçmişi ─────────────────────────────────────────
Write-Step "8. Ödeme Geçmişi (henüz boş olmalı)"
$invoicesResult = Test-Endpoint "GET /api/subscription/invoices" "GET" "$BaseUrl/api/subscription/invoices" $null $token 200
if ($invoicesResult -is [array]) {
    Write-Info "Fatura sayısı: $($invoicesResult.Count)"
} elseif ($invoicesResult) {
    Write-Info "Fatura yanıtı alındı."
}

# ─── 9. Reactivation (iptal yok, hata vermeli) ─────────────────
Write-Step "9. Reactivation (iptal planlanmamış — hata bekleniyor)"
$reactResult = Test-Endpoint "POST /api/subscription/reactivate" "POST" "$BaseUrl/api/subscription/reactivate" @{} $token 400
if ($results[-1].Code -eq 400) {
    Write-Pass "Doğru hata döndü — abonelik zaten aktif veya yok."
}

# ─── 10. Admin: Abonelik İstatistikleri ───────────────────────
Write-Step "10. Admin: Abonelik İstatistikleri"
$statsResult = Test-Endpoint "GET /api/subscription/admin/stats" "GET" "$BaseUrl/api/subscription/admin/stats" $null $token 200
if ($statsResult) {
    Write-Info "MRR: ₺$($statsResult.mrr), Active: $($statsResult.totalActive), Pro: $($statsResult.totalPro)"
}

# ─── SONUÇ ────────────────────────────────────────────────────
Write-Host "`n============================================================" -ForegroundColor Yellow
Write-Host " TEST SONUÇLARI" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Yellow

$passed = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$failed = ($results | Where-Object { $_.Status -eq "FAIL" }).Count
$total = $results.Count

$results | Format-Table -AutoSize

Write-Host "  Toplam: $total | Geçti: $passed | Hata: $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Yellow" })

if ($checkoutResult?.checkoutUrl) {
    Write-Host "`n🔗 İyzico Sandbox Ödeme Sayfası:" -ForegroundColor Cyan
    Write-Host "   $($checkoutResult.checkoutUrl)" -ForegroundColor White
    Write-Host "`n   Sandbox test kartı:" -ForegroundColor Gray
    Write-Host "   Kart No : 5528790000000008" -ForegroundColor White
    Write-Host "   SKT     : 12/30" -ForegroundColor White
    Write-Host "   CVC     : 123" -ForegroundColor White
    Write-Host "   3D Şifre: a" -ForegroundColor White
}
