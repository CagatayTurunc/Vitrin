Write-Host "=== Testing Subscription Endpoints ===" -ForegroundColor Cyan

# Login
Write-Host "`nStep 1: Login..." -ForegroundColor Yellow
$login = docker exec vitrin-auth curl -s -X POST http://localhost:8080/api/auth/login -H "Content-Type: application/json" -d '{\"email\":\"admin@vitrin.com\",\"password\":\"Admin123!\"}'
Write-Host $login

$token = ($login | ConvertFrom-Json).token
Write-Host "`nToken: $token" -ForegroundColor Green

# Get subscription
Write-Host "`nStep 2: Get subscription..." -ForegroundColor Yellow  
$sub = docker exec vitrin-auth curl -s http://localhost:8080/api/subscription/me -H "Authorization: Bearer $token"
Write-Host $sub -ForegroundColor Cyan

# Create checkout
Write-Host "`nStep 3: Create checkout..." -ForegroundColor Yellow
$checkout = docker exec vitrin-auth curl -s -X POST http://localhost:8080/api/subscription/checkout -H "Authorization: Bearer $token" -H "Content-Type: application/json" -d '{\"tier\":1}'
Write-Host $checkout -ForegroundColor Yellow

Write-Host "`n=== Done ===" -ForegroundColor Cyan
