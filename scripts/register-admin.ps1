param(
    [string]$Email = $env:VITRIN_ADMIN_EMAIL,
    [string]$Password = $env:VITRIN_ADMIN_PASSWORD,
    [string]$FullName = "Admin",
    [string]$Username = "vitrinadmin",
    [string]$GatewayUrl = "http://localhost:5000"
)

if ([string]::IsNullOrWhiteSpace($Email)) {
    throw "VITRIN_ADMIN_EMAIL ortam değişkenini veya -Email parametresini sağlayın."
}

if ([string]::IsNullOrWhiteSpace($Password)) {
    $securePassword = Read-Host "Admin parolası" -AsSecureString
    $credential = [System.Net.NetworkCredential]::new("", $securePassword)
    $Password = $credential.Password
}

$body = @{
    email = $Email
    password = $Password
    fullName = $FullName
    username = $Username
} | ConvertTo-Json

$response = Invoke-RestMethod `
    -Uri "$GatewayUrl/api/auth/register" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body

Write-Host "Kayıt tamamlandı." -ForegroundColor Green
$response | ConvertTo-Json
