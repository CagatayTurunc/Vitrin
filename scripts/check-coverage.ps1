param(
    [ValidateRange(0, 100)]
    [double]$Threshold = 35,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$resultsDirectory = Join-Path $root "artifacts\coverage\$timestamp"
$settings = Join-Path $root "tests\coverlet.runsettings"

New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null

$arguments = @(
    "test",
    (Join-Path $root "Vitrin.sln"),
    "--configuration", "Release",
    "--filter", "Category!=Integration&Category!=Contract",
    "--collect", "XPlat Code Coverage",
    "--settings", $settings,
    "--results-directory", $resultsDirectory,
    "--verbosity", "minimal"
)

if ($NoBuild) {
    $arguments += "--no-build"
    $arguments += "--no-restore"
}

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Coverage test run failed with exit code $LASTEXITCODE."
}

$reports = @(Get-ChildItem -LiteralPath $resultsDirectory -Filter "coverage.cobertura.xml" -Recurse)
if ($reports.Count -eq 0) {
    throw "No Cobertura report was produced under $resultsDirectory."
}

# A source line can appear in multiple test-project reports. Keep its highest hit
# count so shared assemblies are not counted more than once.
$sourceLines = @{}
foreach ($report in $reports) {
    [xml]$coverage = Get-Content -LiteralPath $report.FullName -Raw
    foreach ($package in @($coverage.coverage.packages.package)) {
        foreach ($class in @($package.classes.class)) {
            $fileName = ([string]$class.filename).Replace("\", "/")
            if ($fileName -match "(^|/)tests/" -or $fileName -match "(^|/)Migrations/") {
                continue
            }

            foreach ($line in @($class.lines.line)) {
                $key = "$fileName`:$($line.number)"
                $hits = [int]$line.hits
                if (-not $sourceLines.ContainsKey($key) -or $hits -gt $sourceLines[$key]) {
                    $sourceLines[$key] = $hits
                }
            }
        }
    }
}

if ($sourceLines.Count -eq 0) {
    throw "Coverage reports did not contain any Vitrin source lines."
}

$covered = @($sourceLines.Values | Where-Object { $_ -gt 0 }).Count
$total = $sourceLines.Count
$rate = [Math]::Round(($covered / $total) * 100, 2)

Write-Host ""
Write-Host "Coverage: $covered / $total lines = $rate%" -ForegroundColor Cyan
Write-Host "Threshold: $Threshold%" -ForegroundColor Cyan
Write-Host "Reports: $resultsDirectory" -ForegroundColor DarkGray

if ($rate -lt $Threshold) {
    throw "Line coverage $rate% is below the required $Threshold% threshold."
}

Write-Host "Coverage gate passed." -ForegroundColor Green
