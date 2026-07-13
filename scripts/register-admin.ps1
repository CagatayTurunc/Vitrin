# Yeni admin kullanıcısı oluştur
$body = '{"email":"admin@vitrin.app","password":"Admin1234!","fullName":"Admin","username":"vitrinadmin"}'

$response = Invoke-RestMethod `
    -Uri "http://localhost:5000/api/auth/register" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body

Write-Host "Kayit sonucu:" -ForegroundColor Green
$response | ConvertTo-Json
