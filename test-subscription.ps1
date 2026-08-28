# Test script for subscription endpoints

$baseUrl = "http://localhost:8080"

Write-Host "=== Vitrin Subscription Test ===" -ForegroundColor Cyan

# Test 1: Register a new user
Write-Host "`n1. Registering test user..." -ForegroundColor Yellow
$registerBody = @{
    email = "testsubscription@example.com"
    username = "testsubuser"
    fullName = "Test Subscription User"
    password = "TestPass123!"
} | ConvertTo-Json

$registerResponse = docker exec vitrin-auth curl -s -X POST http://localhost:8080/api/auth/register `
    -H "Content-Type: application/json" `
    -d $registerBody

Write-Host "Register response: $registerResponse"

# Test 2: Login
Write-Host "`n2. Logging in..." -ForegroundColor Yellow
$loginBody = @{
    email = "testsubscription@example.com"
    password = "TestPass123!"
} | ConvertTo-Json

$loginResponse = docker exec vitrin-auth curl -s -X POST http://localhost:8080/api/auth/login `
    -H "Content-Type: application/json" `
    -d $loginBody

$loginData = $loginResponse | ConvertFrom-Json
$token = $loginData.token

if ($token) {
    Write-Host "✓ Login successful! Token: $($token.Substring(0,20))..." -ForegroundColor Green
} else {
    Write-Host "✗ Login failed: $loginResponse" -ForegroundColor Red
    exit 1
}

# Test 3: Get current subscription
Write-Host "`n3. Getting current subscription..." -ForegroundColor Yellow
$subscriptionResponse = docker exec vitrin-auth curl -s http://localhost:8080/api/subscription/me `
    -H "Authorization: Bearer $token"

Write-Host "Current subscription:"
$subscriptionResponse | ConvertFrom-Json | ConvertTo-Json -Depth 5
Write-Host ""

# Test 4: Create checkout session for Pro Maker
Write-Host "`n4. Creating checkout session for Pro Maker..." -ForegroundColor Yellow
$checkoutBody = @{
    tier = 1  # ProMaker
} | ConvertTo-Json

$checkoutResponse = docker exec vitrin-auth curl -s -X POST http://localhost:8080/api/subscription/checkout `
    -H "Authorization: Bearer $token" `
    -H "Content-Type: application/json" `
    -d $checkoutBody

Write-Host "Checkout response:"
$checkoutData = $checkoutResponse | ConvertFrom-Json
$checkoutData | ConvertTo-Json -Depth 5

if ($checkoutData.checkoutUrl) {
    Write-Host "`n✓ Checkout URL created successfully!" -ForegroundColor Green
    Write-Host "İyzico Payment URL: $($checkoutData.checkoutUrl)" -ForegroundColor Cyan
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "1. Open this URL in browser"
    Write-Host "2. Use test card: 5528790000000008 | CVV: 123 | Date: 12/30"
    Write-Host "3. Complete 3D Secure"
    Write-Host "4. You'll be redirected back with payment confirmation"
} else {
    Write-Host "✗ Checkout failed: $checkoutResponse" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
