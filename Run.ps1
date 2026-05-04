#Requires -Version 5.1
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

$project = Join-Path $PSScriptRoot '7090ATest\7090ATest.csproj'
$exePath = Join-Path $PSScriptRoot "7090ATest\bin\$Configuration\net472\7090ATest.exe"

Write-Host "Building $project ($Configuration)..." -ForegroundColor Cyan
& dotnet build $project -c $Configuration -nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed (exit $LASTEXITCODE). Not launching." -ForegroundColor Red
    exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $exePath)) {
    Write-Host "Built but exe not found at $exePath" -ForegroundColor Red
    exit 1
}

Write-Host "Launching $exePath in a new window..." -ForegroundColor Green
Start-Process -FilePath $exePath -WorkingDirectory (Split-Path $exePath)
