# GitHub Actions Workflow Manual Trigger
Write-Host "=== GITHUB ACTIONS MANUEL TETİKLEME ===" -ForegroundColor Green

$repo = "CagatayTurunc/Vitrin"
$token = "YOUR_GITHUB_TOKEN"  # GitHub Personal Access Token
$workflow = "deploy.yml"

Write-Host "Workflow tetikleniyor..." -ForegroundColor Yellow

# REST API call to trigger workflow
$headers = @{
    "Authorization" = "token $token"
    "Accept" = "application/vnd.github.v3+json"
}

$body = @{
    "ref" = "main"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/actions/workflows/$workflow/dispatches" -Method Post -Headers $headers -Body $body
    Write-Host "✅ Workflow tetiklendi!" -ForegroundColor Green
    Write-Host "GitHub Actions'da durumu kontrol edin: https://github.com/$repo/actions" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Hata: $_" -ForegroundColor Red
    Write-Host "Manuel olarak GitHub UI'dan tetikleyin:" -ForegroundColor Yellow
    Write-Host "1. https://github.com/$repo/actions" -ForegroundColor White
    Write-Host "2. 'CI/CD Pipeline' seçin" -ForegroundColor White  
    Write-Host "3. 'Run workflow' butonuna tıklayın" -ForegroundColor White
}