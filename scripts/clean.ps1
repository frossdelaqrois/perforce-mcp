[CmdletBinding(SupportsShouldProcess)]
param()

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Split-Path -Parent $PSScriptRoot)).Path
$solution = Join-Path $root 'PerforceMcp.slnx'

if (-not (Test-Path -LiteralPath $solution -PathType Leaf)) {
    throw "Refusing to clean because $solution was not found."
}

$targets = Get-ChildItem -LiteralPath (Join-Path $root 'src'), (Join-Path $root 'tests') -Directory -Recurse -Force |
    Where-Object { $_.Name -in 'bin', 'obj' }
$removedCount = 0

foreach ($target in $targets) {
    $resolved = $target.FullName
    if (-not $resolved.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove a directory outside the repository: $resolved"
    }

    if ($PSCmdlet.ShouldProcess($resolved, 'Remove generated build directory')) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
        $removedCount++
    }
}

if (-not $WhatIfPreference) {
    Write-Host "Removed $removedCount generated build director$(if ($removedCount -eq 1) { 'y' } else { 'ies' })." -ForegroundColor Green
}
