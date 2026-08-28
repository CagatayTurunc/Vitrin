# Simple subscription test with existing user

$baseUrl = "http://localhost:8080"

Write-Host "=== Vitrin Subscription Test (Existing User) ===" -ForegroundColor Cyan

# Use admin@vitrin.com (should exist from setup)
Write-Host "`n1. Logging in as admin..." -ForegroundColor Yellow
$loginBody = '{"email":"admin@vitrin.com","password":"Admin123!"}'

$loginResponse = docker exec vitrin-auth curl -s -X POST http://localhost:8080/api/auth/login `
    -H "Content-Type: application/json" `
    -d $loginBody

Write-Host "Login response: $loginResponse"

try {
    $loginData = $loginResponse | ConvertFrom-Json
    $token = $loginData.token

    if ($token) {
        Write-Host "✓ Login successful!" -ForegroundColor Green
        
        # Get current subscription
        Write-Host "`n2. Getting current subscription..." -ForegroundColor Yellow
        $subscriptionResponse = docker exec vitrin-auth curl -s http://localhost:8080/api/subscription/me `
            -H "Authorization: Bearer $token"
        
        Write-Host "Current subscription: $subscriptionResponse" -ForegroundColor Cyan
        
        # Create checkout
        Write-Host "`n3. Creating checkout for Pro Maker..." -ForegroundColor Yellow
        $checkoutBody = '{"tier":1}'
        
        $checkoutResponse = docker exec vitrin-auth curl -s -X POST http://localhost:8080/api/subscription/checkout `
            -H "Authorization: Bearer $token" `
            -H "Content-Type: application/json" `
            -d $checkoutBody
        
        Write-Host "`nCheckout response:"
        Write-Host $checkoutResponse -ForegroundColor Cyan
        
        try {
            $checkoutData = $checkoutResponse | ConvertFrom-Json
            if ($checkoutData.checkoutUrl) {
                Write-Host "`n✓✓✓ SUCCESS! Checkout URL created!" -ForegroundColor Green
                Write-Host "Payment URL: $($checkoutData.checkoutUrl)" -ForegroundColor Yellow
                Write-Host "`nTest Card: 5528790000000008 | CVV: 123 | Date: 12/30" -ForegroundColor Cyan
            }
        }
        catch {
            Write-Host "Checkout parse error: $_" -ForegroundColor Red
        }
    }
    else {
        Write-Host "✗ Login failed - no token received" -ForegroundColor Red
    }
}
catch {
    Write-Host "✗ Error: $_" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
