[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'PerforceMcp.slnx'
$results = [System.Collections.Generic.List[object]]::new()

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
    Invoke-Stage 'Restore packages' { dotnet restore $solution }
    Invoke-Stage 'Build Release' { dotnet build $solution --configuration Release --no-restore }
    Invoke-Stage 'Run tests' { dotnet test $solution --configuration Release --no-build }
    Invoke-Stage 'Verify formatting' { dotnet format $solution --verify-no-changes --no-restore }
}
catch {
    Write-Host "`nDeveloper update failed: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    Pop-Location
    Write-Host "`nDeveloper update summary" -ForegroundColor Cyan
    $results | Format-Table Stage, Result, @{ Label = 'Duration'; Expression = { '{0:n1}s' -f $_.Duration.TotalSeconds } } -AutoSize
}

if ($results.Count -ne 4 -or $results.Result -contains 'FAIL') {
    exit 1
}

Write-Host 'Developer update completed successfully.' -ForegroundColor Green
