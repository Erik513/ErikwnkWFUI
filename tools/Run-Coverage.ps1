# Measures what the tests reach of the library and writes coverage\summary.md: for every control (a control is all the files that start with its
# name: ListBox.cs, ListBox.Drag.cs ...) the lines, the covered lines and the percentage, the ones under the target first. The tests run on the
# hidden desktop (Run-IsolatedTests.ps1), so nothing is shown or takes the focus.
#
#   .\tools\Run-Coverage.ps1                          the whole test suite (a few minutes; heavy: announce it, do not run it repeatedly)
#   .\tools\Run-Coverage.ps1 -Filter "FullyQualifiedName~Controls.CheckBox"   only some tests (the numbers are then those of these tests)
#   .\tools\Run-Coverage.ps1 -Summarize -Uncovered ListBox   no run: read the last result and list the lines of one control that no test reaches
param(
    [string]$Filter = '',
    [double]$Target = 85,
    [int]$TimeoutSeconds = 3000,
    [switch]$NoBuild,
    [switch]$Summarize,
    [string]$Uncovered = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$raw = Join-Path $root 'coverage\raw'

if (-not $Summarize) {
    if (-not $NoBuild) {
        dotnet build "$root\ErikwnkWFUI.Tests" -f net8.0-windows -v q -m:2 -nodeReuse:false
        if ($LASTEXITCODE -ne 0) { throw "The build failed." }
    }

    if (Test-Path $raw) { Remove-Item $raw -Recurse -Force }

    $arguments = "ErikwnkWFUI.Tests -f net8.0-windows --no-build --collect:`"Code Coverage;Format=cobertura`" --results-directory `"$raw`""
    if ($Filter -ne '') { $arguments += " --filter `"$Filter`"" }

    & "$PSScriptRoot\Run-IsolatedTests.ps1" -Arguments $arguments -WorkingDirectory $root -TimeoutSeconds $TimeoutSeconds
}

$report = Get-ChildItem $raw -Filter '*.cobertura.xml' -Recurse -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $report) { throw "No coverage report under $raw (did the test run finish?)." }

[xml]$xml = Get-Content $report.FullName
$libraryRoot = (Join-Path $root 'ErikwnkWFUI') + '\'

# line -> hits per file (a line of a nested class or a lambda appears several times: the most hits count)
$files = @{}
foreach ($class in $xml.SelectNodes('//class')) {
    $file = $class.filename
    if (-not $file.StartsWith($libraryRoot, [StringComparison]::OrdinalIgnoreCase)) { continue }
    if ($file -match '\\(obj|bin)\\') { continue }
    if (-not $files.ContainsKey($file)) { $files[$file] = @{} }

    foreach ($line in $class.SelectNodes('lines/line')) {
        $number = [int]$line.number
        $hits = [int]$line.hits
        if (-not $files[$file].ContainsKey($number) -or $files[$file][$number] -lt $hits) { $files[$file][$number] = $hits }
    }
}

# the group of a file: the name up to the first dot (ListBox.Drag.cs -> ListBox); the UIStyles.*.cs files are one group
function Get-Group([string]$path) {
    $stem = [IO.Path]::GetFileNameWithoutExtension($path)
    $first = $stem.Split('.')[0]
    return $first
}

$groups = @{}
foreach ($file in $files.Keys) {
    $group = Get-Group $file
    if (-not $groups.ContainsKey($group)) { $groups[$group] = [ordered]@{ Lines = 0; Covered = 0; Files = @() } }
    $groups[$group].Lines += $files[$file].Count
    $groups[$group].Covered += @($files[$file].Values | Where-Object { $_ -gt 0 }).Count
    $groups[$group].Files += $file
}

if ($Uncovered -ne '') {
    if (-not $groups.ContainsKey($Uncovered)) { throw "No group named $Uncovered (the groups are named after the first part of the file name)." }

    foreach ($file in ($groups[$Uncovered].Files | Sort-Object)) {
        $missing = @($files[$file].Keys | Where-Object { $files[$file][$_] -eq 0 } | Sort-Object)
        if ($missing.Count -eq 0) { continue }

        # lines in a row as ranges
        $ranges = @(); $start = $missing[0]; $previous = $start
        foreach ($n in $missing[1..($missing.Count)]) {
            if ($n -eq $previous + 1) { $previous = $n; continue }
            $ranges += $(if ($start -eq $previous) { "$start" } else { "$start-$previous" })
            $start = $n; $previous = $n
        }
        $ranges += $(if ($start -eq $previous) { "$start" } else { "$start-$previous" })
        "{0}: {1}" -f $file.Substring($libraryRoot.Length), ($ranges -join ', ')
    }
    return
}

$rows = foreach ($name in $groups.Keys) {
    $g = $groups[$name]
    $percent = if ($g.Lines -gt 0) { [Math]::Round(100.0 * $g.Covered / $g.Lines, 1) } else { 100 }
    [pscustomobject]@{ Name = $name; Lines = $g.Lines; Covered = $g.Covered; Percent = $percent }
}

$rows = $rows | Sort-Object Percent, Name
$total = ($rows | Measure-Object Lines -Sum).Sum
$covered = ($rows | Measure-Object Covered -Sum).Sum

$out = New-Object System.Text.StringBuilder
[void]$out.AppendLine('# Test coverage of the library')
[void]$out.AppendLine()
[void]$out.AppendLine("Made by tools\Run-Coverage.ps1 from $($report.FullName.Substring($root.Length + 1)) on $(Get-Date -Format 'yyyy-MM-dd HH:mm'). Target: $Target % of the lines per control. A control is all the files that start with its name.")
[void]$out.AppendLine()
[void]$out.AppendLine(("Total: {0} of {1} lines = {2} %" -f $covered, $total, [Math]::Round(100.0 * $covered / [Math]::Max($total, 1), 1).ToString([Globalization.CultureInfo]::InvariantCulture)))
[void]$out.AppendLine()
[void]$out.AppendLine('| Control | Lines | Covered | % | |')
[void]$out.AppendLine('|---|---:|---:|---:|---|')
foreach ($row in $rows) {
    $mark = if ($row.Percent -lt $Target) { 'below the target' } else { '' }
    [void]$out.AppendLine(("| {0} | {1} | {2} | {3} | {4} |" -f $row.Name, $row.Lines, $row.Covered, $row.Percent.ToString([Globalization.CultureInfo]::InvariantCulture), $mark))
}

$summary = Join-Path $root 'coverage\summary.md'
[IO.File]::WriteAllText($summary, $out.ToString(), (New-Object Text.UTF8Encoding $false))

$rows | Format-Table Name, Lines, Covered, Percent -AutoSize | Out-String -Width 200
"Total: $covered of $total lines = $([Math]::Round(100.0 * $covered / [Math]::Max($total, 1), 1)) %   ->  $summary"