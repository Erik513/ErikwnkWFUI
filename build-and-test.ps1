<#
.SYNOPSIS
    Builds the library for both targets, runs the tests on both, then
    rebuilds the Showcase (closing a running one first).

.PARAMETER SkipTests
    Only build.

.PARAMETER SkipShowcase
    Leave the Showcase alone.

.EXAMPLE
    .\build-and-test.ps1
#>
param(
    [switch]$SkipTests,
    [switch]$SkipShowcase
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$targets = 'net48', 'net8.0-windows'

function Invoke-Step([string]$title, [scriptblock]$command) {
    Write-Host "== $title" -ForegroundColor Cyan
    & $command
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAILED: $title" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

foreach ($target in $targets) {
    Invoke-Step "Build library ($target)" { dotnet build ErikwnkWFUI -f $target --nologo -v q }
}

if (-not $SkipTests) {
    foreach ($target in $targets) {
        Invoke-Step "Tests ($target)" { dotnet test ErikwnkWFUI.Tests -f $target --nologo -v q }
    }
}

if (-not $SkipShowcase) {
    Get-Process -Name 'ErikwnkWFUI.Showcase' -ErrorAction SilentlyContinue | Stop-Process -Force
    Invoke-Step 'Build Showcase' { dotnet build ErikwnkWFUI.Showcase --nologo -v q }
}

Write-Host 'All done.' -ForegroundColor Green
