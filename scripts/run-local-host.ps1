param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Dotnet", "Web")]
    [string]$Kind,

    [Parameter(Mandatory = $true)]
    [string]$ServicePath,

    [Parameter(Mandatory = $true)]
    [int]$Port
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$dotenvPath = Join-Path $root ".env"

if (Test-Path $dotenvPath) {
    foreach ($line in Get-Content -LiteralPath $dotenvPath) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith("#") -or -not $trimmed.Contains("=")) { continue }

        $parts = $trimmed.Split("=", 2)
        $name = $parts[0].Trim()
        $value = $parts[1].Trim().Trim('"').Trim("'")
        if ($name) { [Environment]::SetEnvironmentVariable($name, $value, "Process") }
    }
}

if ($env:JWT_SECRET) { $env:Jwt__Secret = $env:JWT_SECRET }
if ($env:RESEND_API_KEY) { $env:Email__Resend__ApiKey = $env:RESEND_API_KEY }
if ($env:EMAIL_FROM) { $env:Email__From = $env:EMAIL_FROM }
if ($env:EMAIL_TOKEN_SECRET) { $env:Email__TokenSecret = $env:EMAIL_TOKEN_SECRET }
if ($env:EMAIL_APP_BASE_URL) { $env:Email__AppBaseUrl = $env:EMAIL_APP_BASE_URL }

Set-Location (Join-Path $root $ServicePath)

if ($Kind -eq "Dotnet") {
    $env:Logging__EventLog__LogLevel__Default = "None"
    & dotnet watch run --no-restore --urls "http://localhost:$Port"
    exit $LASTEXITCODE
}

& .\node_modules\.bin\next.CMD dev --webpack --port $Port
exit $LASTEXITCODE
