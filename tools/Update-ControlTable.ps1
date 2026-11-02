# Brings docs\CONTROLS.md up to date: the table of every control and window of the library, with its properties, events, methods and factory
# methods, made from the built library (see ErikwnkWFUI.Tests\Docs\ControlTableGenerator.cs). The text that cannot be read from the code (the
# standard properties a control ignores, a factory class with another name) is in docs\control-notes.json.
#
# The test ControlTableTests fails while the file is out of date; run this script and commit the file. It builds the test project once (use
# -NoBuild when it was built already) and runs the generator test on the hidden desktop, like all tests.
param(
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

if (-not $NoBuild) {
    dotnet build "$root\ErikwnkWFUI.Tests" -f net8.0-windows -v q -m:2 -nodeReuse:false
    if ($LASTEXITCODE -ne 0) { throw "The build failed." }
}

$env:UPDATE_CONTROLS_MD = '1'
try {
    & "$PSScriptRoot\Run-IsolatedTests.ps1" -Arguments "ErikwnkWFUI.Tests -f net8.0-windows --no-build --filter FullyQualifiedName~ControlTableTests" -WorkingDirectory $root -TimeoutSeconds 300
}
finally {
    Remove-Item Env:UPDATE_CONTROLS_MD -ErrorAction SilentlyContinue
}

Get-Item "$root\docs\CONTROLS.md" | Select-Object FullName, Length, LastWriteTime