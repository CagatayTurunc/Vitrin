Write-Host "Starting Vitrin Microservices..." -ForegroundColor Green

$projects = @(
    "src\Gateways\Vitrin.Gateway",
    "src\Services\Auth\Vitrin.Auth.Api",
    "src\Services\Product\Vitrin.Product.Api",
    "src\Services\Voting\Vitrin.Voting.Api",
    "src\Services\Comment\Vitrin.Comment.Api",
    "src\Services\Notification\Vitrin.Notification.Api",
    "src\Services\Analytics\Vitrin.Analytics.Api",
    "src\Services\Ai\Vitrin.Ai.Api"
)

foreach ($proj in $projects) {
    Write-Host "Starting $proj ..." -ForegroundColor Cyan
    Start-Process -FilePath "dotnet" -ArgumentList "run --project $proj" -WindowStyle Minimized
}

Write-Host "All services started!" -ForegroundColor Green
