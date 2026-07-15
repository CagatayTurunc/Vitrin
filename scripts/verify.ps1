param(
    [switch]$SkipRestore,
    [switch]$SkipInstall,
    [switch]$SkipBuild,
    [switch]$SkipIntegration,
    [switch]$Coverage,
    [switch]$E2E,
    [switch]$CheckCompose
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$web = Join-Path $root "src\Web\Vitrin.Web.UI"

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)]
        [string]$Label,
        [Parameter(Mandatory)]
        [scriptblock]$Command
    )

    Write-Host "`n==> $Label" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Label başarısız oldu (çıkış kodu: $LASTEXITCODE)."
    }
}

Push-Location $root
try {
    if (-not $SkipRestore) {
        Invoke-CheckedCommand "Backend bağımlılıklarını geri yükleme" {
            dotnet restore Vitrin.sln
        }
    }

    Invoke-CheckedCommand "Backend unit testleri" {
        dotnet test Vitrin.sln --configuration Release --no-restore --verbosity minimal --filter "Category!=Integration&Category!=Contract"
    }

    if (-not $SkipIntegration) {
        Invoke-CheckedCommand "Backend integration ve contract testleri" {
            dotnet test tests\Vitrin.IntegrationTests\Vitrin.IntegrationTests.csproj --configuration Release --no-restore --verbosity minimal --filter "Category=Integration|Category=Contract"
        }
    }

    if ($Coverage) {
        Invoke-CheckedCommand "Backend coverage eşiği" {
            & (Join-Path $PSScriptRoot "check-coverage.ps1") -NoBuild
        }
    }

    Push-Location $web
    try {
        if (-not $SkipInstall) {
            Invoke-CheckedCommand "Frontend bağımlılık doğrulaması" {
                corepack pnpm install --frozen-lockfile
            }
        }

        Invoke-CheckedCommand "Frontend lint" {
            corepack pnpm lint
        }

        Invoke-CheckedCommand "Frontend typecheck" {
            corepack pnpm typecheck
        }

        Invoke-CheckedCommand "Frontend unit testleri" {
            corepack pnpm test
        }

        if ($Coverage) {
            Invoke-CheckedCommand "Frontend coverage eşiği" {
                corepack pnpm test:coverage
            }
        }

        if ($E2E) {
            Invoke-CheckedCommand "Frontend Playwright smoke ve accessibility testleri" {
                corepack pnpm test:e2e
            }
        }

        if (-not $SkipBuild) {
            Invoke-CheckedCommand "Frontend production build" {
                corepack pnpm build
            }
        }
    }
    finally {
        Pop-Location
    }

    if ($CheckCompose) {
        if (-not (Test-Path -LiteralPath (Join-Path $root ".env"))) {
            throw "Compose doğrulaması için .env.example dosyasını .env olarak kopyalayıp CHANGE_ME değerlerini değiştirin."
        }

        Invoke-CheckedCommand "Docker Compose yapılandırması" {
            docker compose config --quiet
        }
    }
}
finally {
    Pop-Location
}

Write-Host "`nTüm kalite kapıları başarıyla geçti." -ForegroundColor Green
