Write-Host "=== FINAL Subscription Test ===" -ForegroundColor Cyan

# Register new user
Write-Host "`n1. Registering new user..." -ForegroundColor Yellow
$register = docker exec vitrin-auth curl -s -X POST http://localhost:8080/api/auth/register -H "Content-Type: application/json" -d '{\"email\":\"premium@test.com\",\"username\":\"premiumuser\",\"fullName\":\"Premium Test User\",\"password\":\"TestPass123!\"}'
Write-Host $register

# Login
Write-Host "`n2. Logging in..." -ForegroundColor Yellow
$login = docker exec vitrin-auth curl -s -X POST http://localhost:8080/api/auth/login -H "Content-Type: application/json" -d '{\"email\":\"premium@test.com\",\"password\":\"TestPass123!\"}'
Write-Host $login

$loginJson = $login | ConvertFrom-Json
$token = $loginJson.token

if ($token) {
    Write-Host "`n✓ Token received: $($token.Substring(0,30))..." -ForegroundColor Green
    
    # Get subscription (should be Free tier by default)
    Write-Host "`n3. Getting current subscription..." -ForegroundColor Yellow
    $sub = docker exec vitrin-auth curl -s http://localhost:8080/api/subscription/me -H "Authorization: Bearer $token"
    Write-Host $sub -ForegroundColor Cyan
    
    # Create checkout for Pro Maker (tier: 1)
    Write-Host "`n4. Creating checkout for Pro Maker..." -ForegroundColor Yellow
    $checkout = docker exec vitrin-auth curl -s -X POST http://localhost:8080/api/subscription/checkout -H "Authorization: Bearer $token" -H "Content-Type: application/json" -d '{\"tier\":1}'
    Write-Host $checkout
    
    $checkoutJson = $checkout | ConvertFrom-Json
    if ($checkoutJson.checkoutUrl) {
        Write-Host "`n✓✓✓ SUCCESS! Payment page created!" -ForegroundColor Green
        Write-Host "`nİyzico Payment URL:" -ForegroundColor Yellow
        Write-Host $checkoutJson.checkoutUrl -ForegroundColor Cyan
        Write-Host "`nTest Card Details:" -ForegroundColor Yellow
        Write-Host "Card: 5528790000000008" -ForegroundColor Cyan
        Write-Host "CVV: 123" -ForegroundColor Cyan
        Write-Host "Expiry: 12/30" -ForegroundColor Cyan
        Write-Host "`nNext Steps:" -ForegroundColor Yellow
        Write-Host "1. Copy the URL above and open in browser"
        Write-Host "2. Enter test card details"
        Write-Host "3. Complete 3D Secure verification"
        Write-Host "4. System will upgrade subscription automatically"
    }
} else {
    Write-Host "`n✗ Login failed" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
