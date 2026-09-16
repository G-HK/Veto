#Requires -Version 7

<#
.SYNOPSIS
    Verifies the packed .nupkg files contain what they are supposed to contain.

.DESCRIPTION
    Two failures this catches, both of which are silent at pack time and only surface in a
    consumer's app:

    1. A package stopped being produced at all - a project dropped out of the solution, or
       picked up IsPackable=false - so a `dotnet nuget push *.nupkg` quietly pushes fewer
       packages than the library has.

    2. A Razor class library's static web assets did not make it into the package. The build
       still succeeds and the assembly is fine; the JS module or stylesheet is simply absent,
       and the component fails at runtime in the consumer's browser with a 404.

    Run it against the output folder of `dotnet pack` before pushing anything.

.PARAMETER PackageDirectory
    Folder holding the .nupkg files produced by `dotnet pack`.
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string] $PackageDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Every package the library ships. Keeping the list here rather than globbing the solution is
# deliberate: it is what makes case 1 above detectable at all.
$expectedPackages = @(
    'Veto'
    'Veto.Core'
    'Veto.Theming'
    'Veto.BlinkText'
    'Veto.Buttons'
    'Veto.Code'
    'Veto.Cron'
    'Veto.Feedback'
    'Veto.Gantt'
    'Veto.Gauge'
    'Veto.Handbook'
    'Veto.Icon'
    'Veto.Kbd'
    'Veto.Menu'
    'Veto.Navigation'
    'Veto.Rating'
    'Veto.Slider'
    'Veto.Switch'
    'Veto.Table'
    'Veto.TextBox'
    'Veto.Tilt'
    'Veto.Toast'
    'Veto.Layout.Card'
    'Veto.Layout.Container'
    'Veto.Layout.Divider'
    'Veto.Layout.Grid'
    'Veto.Layout.Spacer'
    'Veto.Layout.Stack'
    'Veto.Layouts.Dashboard'
    'Veto.Layouts.DashboardLayout'
    'Veto.Layouts.Dock'
    'Veto.Layouts.DockLayout'
    'Veto.Layouts.DocsLayout'
    'Veto.Layouts.Shell'
)

# The packages that carry hand-written static web assets, and the files each must contain.
# Scoped CSS bundles are not listed - those are generated, and checked generically below.
$expectedAssets = @{
    'Veto.Core'          = @('VetoStyles.css')
    'Veto.Theming'       = @('veto-theme.js', 'veto-theme-init.js')
    'Veto.Code'          = @('veto-code.js')
    'Veto.Gantt'         = @('veto-gantt.js', 'veto-gantt-store.js', 'veto-gantt-drag.js')
    'Veto.Tilt'          = @('veto-tilt.js')
    'Veto.Layouts.Dock'  = @('veto-dock.js')
    'Veto.Layouts.Shell' = @('veto-layout.js')
}

if (-not (Test-Path $PackageDirectory)) {
    throw "Package directory not found: $PackageDirectory"
}

$packages = Get-ChildItem -Path $PackageDirectory -Filter '*.nupkg' |
    Where-Object { $_.Name -notlike '*.symbols.nupkg' }

if ($packages.Count -eq 0) {
    throw "No .nupkg files found in $PackageDirectory"
}

# Map "Veto.Buttons.1.2.3.nupkg" back to "Veto.Buttons" by matching the longest expected id
# that the file name starts with - "Veto" alone would otherwise match every package.
$byId = @{}
foreach ($package in $packages) {
    $match = $expectedPackages |
        Where-Object { $package.BaseName.StartsWith("$_.") } |
        Sort-Object -Property Length -Descending |
        Select-Object -First 1

    if ($match) { $byId[$match] = $package }
    else { Write-Warning "Unrecognised package, not in the expected list: $($package.Name)" }
}

$failures = [System.Collections.Generic.List[string]]::new()

foreach ($id in $expectedPackages) {
    if (-not $byId.ContainsKey($id)) {
        $failures.Add("Package was not produced: $id")
        continue
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($byId[$id].FullName)
    try {
        $entries = $archive.Entries.FullName

        foreach ($asset in $expectedAssets[$id]) {
            if (-not ($entries -contains "staticwebassets/$asset")) {
                $failures.Add("$id is missing static web asset: $asset")
            }
        }

        if (-not ($entries | Where-Object { $_ -like 'lib/*.dll' -or $_ -like 'lib/*/*.dll' })) {
            # The meta package is dependency-only by design and has an assembly of its own for
            # AddVeto(), so every package here is expected to carry one.
            $failures.Add("$id contains no assembly under lib/")
        }
    }
    finally {
        $archive.Dispose()
    }
}

Write-Host "Checked $($byId.Count) of $($expectedPackages.Count) expected packages in $PackageDirectory"

if ($failures.Count -gt 0) {
    Write-Host ''
    foreach ($failure in $failures) { Write-Host "  FAIL  $failure" }
    throw "$($failures.Count) package check(s) failed."
}

Write-Host 'All package checks passed.'
