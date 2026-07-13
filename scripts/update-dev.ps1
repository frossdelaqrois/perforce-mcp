[CmdletBinding()]
param(
    [switch] $Pull
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'PerforceMcp.slnx'
$serverOutput = Join-Path $root 'src\PerforceMcp.Server\bin\Release'
$results = [System.Collections.Generic.List[object]]::new()

function Get-ServerDllFingerprint {
    if (-not (Test-Path -LiteralPath $serverOutput -PathType Container)) {
        return $null
    }

    $entries = Get-ChildItem -LiteralPath $serverOutput -Filter '*.dll' -File -Recurse |
        Sort-Object FullName |
        ForEach-Object {
            $relativePath = [IO.Path]::GetRelativePath($serverOutput, $_.FullName)
            "$relativePath=$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)"
        }

    if (-not $entries) {
        return $null
    }

    return $entries -join "`n"
}

function Invoke-Stage {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [scriptblock] $Action
    )

    Write-Host "`n==> $Name" -ForegroundColor Cyan
    $started = Get-Date
    try {
        & $Action
        if ($LASTEXITCODE -ne 0) {
            throw "$Name exited with code $LASTEXITCODE."
        }
        $results.Add([pscustomobject]@{ Stage = $Name; Result = 'PASS'; Duration = (Get-Date) - $started })
    }
    catch {
        $results.Add([pscustomobject]@{ Stage = $Name; Result = 'FAIL'; Duration = (Get-Date) - $started })
        throw
    }
}

try {
    Push-Location $root

    $branch = (git branch --show-current).Trim()
    $commit = (git rev-parse --short HEAD).Trim()
    $version = (git describe --tags --always --dirty).Trim()
    $changes = @(git status --porcelain)
    $isClean = $changes.Count -eq 0

    Write-Host 'Developer workspace' -ForegroundColor Cyan
    Write-Host "  Branch:  $branch"
    Write-Host "  Commit:  $commit"
    Write-Host "  Version: $version"
    Write-Host "  Git:     $(if ($isClean) { 'clean' } else { "dirty ($($changes.Count) change(s))" })" -ForegroundColor $(if ($isClean) { 'Green' } else { 'Yellow' })

    if ($Pull) {
        if (-not $isClean) {
            throw 'Cannot pull while the Git working tree has local changes.'
        }
        Invoke-Stage 'Pull latest changes' { git pull --ff-only }
    }

    $dllFingerprintBefore = Get-ServerDllFingerprint
    Invoke-Stage 'Restore packages' { dotnet restore $solution --locked-mode }
    Invoke-Stage 'Build Release' { dotnet build $solution --configuration Release --no-restore }
    Invoke-Stage 'Run tests' { dotnet test $solution --configuration Release --no-build }
    Invoke-Stage 'Verify formatting' { dotnet format $solution --verify-no-changes --no-restore }
    $dllFingerprintAfter = Get-ServerDllFingerprint
    $serverDllChanged = $dllFingerprintBefore -ne $dllFingerprintAfter
}
catch {
    Write-Host "`nDeveloper update failed: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    Pop-Location
    Write-Host "`nDeveloper update summary" -ForegroundColor Cyan
    $results | Format-Table Stage, Result, @{ Label = 'Duration'; Expression = { '{0:n1}s' -f $_.Duration.TotalSeconds } } -AutoSize
}

$expectedStageCount = if ($Pull) { 5 } else { 4 }
if ($results.Count -ne $expectedStageCount -or $results.Result -contains 'FAIL') {
    exit 1
}

if ($serverDllChanged) {
    Write-Host 'MCP server DLL output changed. Restart Codex before testing.' -ForegroundColor Yellow
}
else {
    Write-Host 'MCP server DLL output is unchanged.' -ForegroundColor DarkGray
}

Write-Host 'Ready to test.' -ForegroundColor Green
